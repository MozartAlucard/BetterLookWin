using System;
using Microsoft.Win32;

namespace BetterLookWin.Core;

/// <summary>
/// "Iniciar con Windows": usa la clave Run del usuario actual (no pide permisos de administrador).
/// Así aparece en el Administrador de tareas > Aplicaciones de inicio, y desde ahí también se puede desactivar.
/// </summary>
internal static class StartupManager
{
    /// <summary>Argumento con el que Windows lanza la app al iniciar: arranca directo a la bandeja, sin imagen de bienvenida ni ventana.</summary>
    public const string BackgroundArg = "--background";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "BetterLookWin";

    private static string Command => $"\"{Environment.ProcessPath}\" {BackgroundArg}";

    /// <summary>True si está registrada y el Administrador de tareas no la tiene desactivada.</summary>
    public static bool IsEnabled()
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            if (run?.GetValue(ValueName) is not string) return false;

            // El Administrador de tareas desactiva una entrada sin borrarla: lo marca en StartupApproved
            // (primer byte con el bit 1 encendido = desactivada).
            using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey);
            if (approved?.GetValue(ValueName) is byte[] state && state.Length > 0 && (state[0] & 1) == 1)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Activa o desactiva el inicio con Windows. Devuelve false si no se pudo.</summary>
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                using var run = Registry.CurrentUser.CreateSubKey(RunKey);
                run.SetValue(ValueName, Command, RegistryValueKind.String);
                ClearApproved();   // si estaba desactivada desde el Administrador de tareas, vuelve a quedar activa
            }
            else
            {
                using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                run?.DeleteValue(ValueName, throwOnMissingValue: false);
                ClearApproved();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Si la carpeta de la app cambió de lugar, actualiza la ruta registrada (respeta si estaba desactivada).</summary>
    public static void RefreshPath()
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (run?.GetValue(ValueName) is string current && !string.Equals(current, Command, StringComparison.OrdinalIgnoreCase))
                run.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        catch
        {
        }
    }

    /// <summary>True si la app corre desde una carpeta de compilación de desarrollo (bin\...\Debug): no la registramos ni tocamos su ruta.</summary>
    public static bool IsDevelopmentBuild()
    {
        string path = Environment.ProcessPath ?? "";
        return path.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase) &&
               path.Contains(@"\Debug\", StringComparison.OrdinalIgnoreCase);
    }

    private static void ClearApproved()
    {
        using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
