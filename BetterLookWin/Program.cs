using System;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using BetterLookWin.Core;
using BetterLookWin.Interop;

namespace BetterLookWin;

internal static class Program
{
    private const string ShowSettingsEvent = "BetterLookWin.ShowSettings";

    [STAThread]
    private static void Main(string[] args)
    {
        // Por-monitor DPI v2: las coordenadas del mouse y de UI Automation son píxeles reales.
        Native.SetProcessDpiAwarenessContext(new IntPtr(-4));

        using var mutex = new Mutex(true, "BetterLookWin.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            // Ya está corriendo (por ejemplo, iniciada con Windows): le pedimos que abra su ventana.
            try
            {
                using var existing = EventWaitHandle.OpenExisting(ShowSettingsEvent);
                existing.Set();
            }
            catch
            {
            }
            return;
        }

        // Cuando Windows la inicia sola, arranca directo a la bandeja (sin imagen de bienvenida ni ventana).
        bool background = args.Contains(StartupManager.BackgroundArg, StringComparer.OrdinalIgnoreCase);

        var app = new System.Windows.Application
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
        };

        FeatureHost? host = null;
        TrayController? tray = null;
        System.Collections.Generic.IReadOnlyList<string> loadErrors = Array.Empty<string>();

        // Una segunda copia del programa avisa por aquí para que esta abra la ventana principal.
        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEvent);
        new Thread(() =>
        {
            while (showEvent.WaitOne())
                app.Dispatcher.BeginInvoke(new Action(() => tray?.OpenSettings()));
        })
        { IsBackground = true }.Start();

        void Boot()
        {
            var settings = SettingsStore.Load();

            // Primera vez: la app queda iniciando con Windows (se puede desactivar en la ventana, en el menú
            // de la bandeja o en el Administrador de tareas). Las compilaciones de desarrollo no se registran solas.
            if (!settings.StartupInitialized && !StartupManager.IsDevelopmentBuild())
            {
                StartupManager.SetEnabled(true);
                settings.StartupInitialized = true;
                settings.Save();
            }
            // Una copia de desarrollo no debe cambiar la ruta registrada de la copia publicada.
            if (!StartupManager.IsDevelopmentBuild()) StartupManager.RefreshPath();

            host = new FeatureHost(settings);
            loadErrors = host.LoadInstalled();
            tray = new TrayController(host);
            host.StartEnabled();
        }

        if (background)
        {
            Boot();
        }
        else
        {
            // 1) La imagen de bienvenida se muestra primero; 2) cuando se cierra, se abre la ventana principal.
            var splash = new SplashWindow();
            splash.Closed += (_, _) =>
            {
                tray?.OpenSettings();

                if (loadErrors.Count > 0)
                {
                    System.Windows.MessageBox.Show(
                        "Algunas funciones instaladas no se pudieron cargar:\n\n" + string.Join("\n", loadErrors),
                        "Better Look Windows", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            };
            splash.Show();

            // La carga real ocurre justo después de pintar la imagen; la animación arranca cuando todo está listo.
            app.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                Boot();
                splash.Begin();
            }));
        }

        app.Exit += (_, _) => host?.StopAll();
        app.Run();
        tray?.Dispose();
    }
}
