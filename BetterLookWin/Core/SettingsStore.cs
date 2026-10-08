using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BetterLookWin.Core;

/// <summary>
/// Guarda en %AppData%\BetterLookWin\settings.json si cada función está activada
/// y sus opciones (un objeto tipado por función).
/// </summary>
internal sealed class SettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BetterLookWin",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed class Data
    {
        public Dictionary<string, bool> Enabled { get; set; } = new();
        public Dictionary<string, JsonElement> Options { get; set; } = new();
        public bool StartupInitialized { get; set; }
        public string? FeaturesParentFolder { get; set; }
    }

    private Data _data = new();

    public static SettingsStore Load()
    {
        var store = new SettingsStore();
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                store._data = JsonSerializer.Deserialize<Data>(json) ?? new Data();
            }
        }
        catch
        {
            // Archivo dañado: arrancamos con valores por defecto.
            store._data = new Data();
        }
        return store;
    }

    /// <summary>True cuando ya se decidió (la primera vez) si la app inicia con Windows.</summary>
    public bool StartupInitialized
    {
        get => _data.StartupInitialized;
        set => _data.StartupInitialized = value;
    }

    /// <summary>Carpeta elegida por el usuario donde se crea "features", o null para usar la de %AppData%.</summary>
    public string? FeaturesParentFolder
    {
        get => _data.FeaturesParentFolder;
        set => _data.FeaturesParentFolder = value;
    }

    public bool IsEnabled(string featureId, bool defaultValue = true) =>
        _data.Enabled.TryGetValue(featureId, out var value) ? value : defaultValue;

    public void SetEnabled(string featureId, bool enabled) =>
        _data.Enabled[featureId] = enabled;

    public T GetOptions<T>(string featureId) where T : new()
    {
        try
        {
            if (_data.Options.TryGetValue(featureId, out var element))
                return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? new T();
        }
        catch
        {
        }
        return new T();
    }

    public void SetOptions<T>(string featureId, T options) =>
        _data.Options[featureId] = JsonSerializer.SerializeToElement(options);

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data, JsonOptions));
        }
        catch
        {
            // Si no se puede guardar, la app sigue funcionando con lo que tiene en memoria.
        }
    }
}
