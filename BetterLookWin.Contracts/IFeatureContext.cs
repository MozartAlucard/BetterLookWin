namespace BetterLookWin.Contracts;

/// <summary>
/// Lo que BetterLookWin le entrega a cada feature externa al crearla.
/// Declara un constructor público que reciba esto (o uno sin parámetros si no lo necesitas).
/// </summary>
public interface IFeatureContext
{
    /// <summary>Identificador de la feature.</summary>
    string FeatureId { get; }

    /// <summary>Carpeta donde está instalada la feature (solo lectura por convención).</summary>
    string InstallFolder { get; }

    /// <summary>Carpeta propia para guardar archivos de la feature. Se crea sola y sobrevive a las actualizaciones.</summary>
    string DataFolder { get; }

    /// <summary>Lee las opciones guardadas de la feature (o una instancia nueva con sus valores por defecto).</summary>
    T GetOptions<T>() where T : new();

    /// <summary>Guarda las opciones; BetterLookWin las escribe a disco al cerrar la configuración.</summary>
    void SetOptions<T>(T options);
}
