using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BetterLookWin.Contracts;
using BetterLookWin.Packages;

namespace BetterLookWin.Core;

/// <summary>
/// Reúne las funciones integradas y las instaladas desde ZIP, las enciende y apaga,
/// y se encarga de instalar y desinstalar paquetes.
/// </summary>
internal sealed class FeatureHost
{
    /// <summary>Carpeta por defecto (%AppData%\BetterLookWin) dentro de la cual se crea "features".</summary>
    public static readonly string DefaultParentFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetterLookWin");

    /// <summary>Carpeta donde viven las funciones instaladas (la ubicación se puede cambiar en la ventana).</summary>
    public string FeaturesFolder => Path.Combine(Settings.FeaturesParentFolder ?? DefaultParentFolder, "features");

    private readonly List<IFeature> _features = new();
    private readonly Dictionary<string, InstalledPackage> _packages = new(StringComparer.OrdinalIgnoreCase);

    public SettingsStore Settings { get; }
    public IReadOnlyList<IFeature> Features => _features;

    /// <summary>Se dispara cuando una función se activa o desactiva.</summary>
    public event Action? FeatureStateChanged;

    /// <summary>Se dispara cuando se instala, actualiza o desinstala una función.</summary>
    public event Action? FeaturesChanged;

    public FeatureHost(SettingsStore settings)
    {
        Settings = settings;
        _features.AddRange(FeatureCatalog.CreateAll(settings));
    }

    public bool IsEnabled(IFeature feature) => Settings.IsEnabled(feature.Id);

    /// <summary>El paquete instalado de esta función, o null si es una función integrada.</summary>
    public InstalledPackage? FindPackage(string featureId) =>
        _packages.TryGetValue(featureId, out var package) ? package : null;

    // ---------- Arranque ----------

    /// <summary>Carga las funciones instaladas desde ZIP. Devuelve los errores (una línea por paquete).</summary>
    public IReadOnlyList<string> LoadInstalled()
    {
        var errors = new List<string>();
        if (!Directory.Exists(FeaturesFolder)) return errors;

        foreach (string dir in Directory.GetDirectories(FeaturesFolder))
        {
            string name = Path.GetFileName(dir);

            // Restos de instalaciones interrumpidas o carpetas por borrar.
            if (name.StartsWith('.'))
            {
                try { Directory.Delete(dir, true); } catch { }
                continue;
            }

            try
            {
                var package = PackageStore.LoadFolder(dir, Settings);
                if (_features.Any(f => string.Equals(f.Id, package.Manifest.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    package.Context.Unload();
                    throw new InvalidDataException("Ya existe otra función con ese identificador.");
                }

                _packages[package.Manifest.Id] = package;
                _features.Add(package.Feature);
            }
            catch (Exception ex)
            {
                errors.Add($"{name}: {ex.Message}");
            }
        }

        return errors;
    }

    public void StartEnabled()
    {
        foreach (var feature in _features.ToList())
            if (IsEnabled(feature)) SafeStart(feature);
    }

    public void StopAll()
    {
        foreach (var feature in _features.ToList()) SafeStop(feature);
        Settings.Save();
    }

    // ---------- Encender / apagar ----------

    public void SetEnabled(IFeature feature, bool enabled)
    {
        Settings.SetEnabled(feature.Id, enabled);
        Settings.Save();

        if (enabled) SafeStart(feature);
        else SafeStop(feature);

        FeatureStateChanged?.Invoke();
    }

    // ---------- Instalar / desinstalar ----------

    public PackageResult Install(string zipPath, PackageInspection inspection)
    {
        if (!inspection.IsValid || inspection.Manifest == null)
            return new PackageResult(false, inspection.Error ?? "ZIP no admitido.");

        var manifest = inspection.Manifest;
        _packages.TryGetValue(manifest.Id, out var existing);

        if (existing == null && _features.Any(f => string.Equals(f.Id, manifest.Id, StringComparison.OrdinalIgnoreCase)))
            return new PackageResult(false, "Ya existe una función integrada con ese identificador.");

        string staging = Path.Combine(FeaturesFolder, ".staging-" + Guid.NewGuid().ToString("N"));
        string target = Path.Combine(FeaturesFolder, manifest.Id);

        try
        {
            Directory.CreateDirectory(FeaturesFolder);
            PackageStore.Extract(zipPath, inspection.RootPrefix, staging);

            // Prueba de carga desde la carpeta temporal: si algo falla, no se toca lo que ya estaba instalado.
            var probe = PackageStore.LoadFolder(staging, Settings);
            probe.Context.Unload();

            if (existing != null) RemovePackage(existing);
            else DiscardFolder(target);

            Directory.Move(staging, target);

            var package = PackageStore.LoadFolder(target, Settings);
            _packages[manifest.Id] = package;
            _features.Add(package.Feature);
            if (IsEnabled(package.Feature)) SafeStart(package.Feature);

            FeaturesChanged?.Invoke();

            return new PackageResult(true, existing == null
                ? $"«{manifest.Name}» quedó instalada."
                : $"«{manifest.Name}» se actualizó a la versión {manifest.Version}.");
        }
        catch (Exception ex)
        {
            DiscardFolder(staging);
            return new PackageResult(false, "No se pudo instalar: " + ex.Message);
        }
    }

    public PackageResult Uninstall(IFeature feature)
    {
        if (!_packages.TryGetValue(feature.Id, out var package))
            return new PackageResult(false, "Es una función integrada: no se puede desinstalar.");

        RemovePackage(package);
        FeaturesChanged?.Invoke();
        return new PackageResult(true, $"«{feature.Name}» se desinstaló.");
    }

    private void RemovePackage(InstalledPackage package)
    {
        SafeStop(package.Feature);
        _features.Remove(package.Feature);
        _packages.Remove(package.Manifest.Id);
        package.Context.Unload();
        DiscardFolder(package.Folder);
    }

    /// <summary>Borra una carpeta; si algo la tiene abierta, la aparta y se limpia en el próximo arranque.</summary>
    private void DiscardFolder(string folder)
    {
        if (!Directory.Exists(folder)) return;

        string current = folder;
        try
        {
            string trash = Path.Combine(FeaturesFolder, ".trash-" + Guid.NewGuid().ToString("N"));
            Directory.Move(folder, trash);
            current = trash;
        }
        catch
        {
        }

        try { Directory.Delete(current, true); } catch { }
    }

    // ---------- Ubicación de la carpeta de funciones ----------

    /// <summary>
    /// Crea la carpeta "features" dentro de <paramref name="newParent"/> y pasa a usarla.
    /// Si <paramref name="moveInstalled"/> es true, copia ahí las funciones ya instaladas.
    /// </summary>
    public PackageResult ChangeFeaturesFolder(string newParent, bool moveInstalled)
    {
        try
        {
            string oldFolder = Path.GetFullPath(FeaturesFolder);
            string newFolder = Path.GetFullPath(Path.Combine(newParent, "features"));

            if (string.Equals(oldFolder, newFolder, StringComparison.OrdinalIgnoreCase))
                return new PackageResult(true, "Esa ya es la carpeta de funciones.");

            string oldWithSep = oldFolder + Path.DirectorySeparatorChar;
            string newWithSep = newFolder + Path.DirectorySeparatorChar;
            if (newWithSep.StartsWith(oldWithSep, StringComparison.OrdinalIgnoreCase) ||
                oldWithSep.StartsWith(newWithSep, StringComparison.OrdinalIgnoreCase))
                return new PackageResult(false, "Elige una ubicación fuera de la carpeta de funciones actual.");

            Directory.CreateDirectory(newFolder);

            // Se apagan y sueltan las funciones instaladas antes de moverlas.
            foreach (var package in _packages.Values.ToList()) RemoveFromMemory(package);

            if (moveInstalled && Directory.Exists(oldFolder))
            {
                foreach (string dir in Directory.GetDirectories(oldFolder))
                {
                    string name = Path.GetFileName(dir);
                    if (name.StartsWith('.')) continue;

                    string target = Path.Combine(newFolder, name);
                    if (Directory.Exists(target)) Directory.Delete(target, true);
                    CopyDirectory(dir, target);
                    try { Directory.Delete(dir, true); } catch { }
                }
            }

            Settings.FeaturesParentFolder = newParent;
            Settings.Save();

            var errors = LoadInstalled();
            StartEnabled();
            FeaturesChanged?.Invoke();

            string message = $"La carpeta de funciones ahora es:\n{newFolder}";
            if (errors.Count > 0) message += "\n\nAlgunas funciones no se pudieron cargar:\n" + string.Join("\n", errors);
            return new PackageResult(true, message);
        }
        catch (Exception ex)
        {
            return new PackageResult(false, "No se pudo cambiar la carpeta: " + ex.Message);
        }
    }

    private void RemoveFromMemory(InstalledPackage package)
    {
        SafeStop(package.Feature);
        _features.Remove(package.Feature);
        _packages.Remove(package.Manifest.Id);
        package.Context.Unload();
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        foreach (string dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }

    // ---------- Seguridad al arrancar / parar ----------

    private static void SafeStart(IFeature feature)
    {
        if (feature.IsRunning) return;
        try
        {
            feature.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo iniciar \"{feature.Name}\":\n\n{ex.Message}",
                "Better Look Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static void SafeStop(IFeature feature)
    {
        if (!feature.IsRunning) return;
        try
        {
            feature.Stop();
        }
        catch
        {
        }
    }
}
