using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BetterLookWin.Core;

/// <summary>Imágenes de la app (carpeta Assets, incluidas como recursos del programa).</summary>
internal static class AppAssets
{
    private static Uri Pack(string file) => new($"pack://application:,,,/Assets/{file}", UriKind.Absolute);

    /// <summary>Ícono para ventanas WPF (esquina y barra de tareas).</summary>
    public static BitmapFrame WindowIcon()
    {
        var frame = BitmapFrame.Create(Pack("icon.png"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        frame.Freeze();
        return frame;
    }

    /// <summary>Imagen de bienvenida.</summary>
    public static BitmapImage Splash()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = Pack("splash.png");
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>Ícono de la bandeja del sistema, al tamaño que pide Windows (16 px a 100%, más a mayor escala).</summary>
    public static Icon TrayIcon()
    {
        try
        {
            var info = Application.GetResourceStream(Pack("icon.ico"));
            if (info != null)
            {
                using var stream = info.Stream;
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Position = 0;
                return new Icon(memory, System.Windows.Forms.SystemInformation.SmallIconSize);
            }
        }
        catch
        {
            // Si falta el recurso, la app sigue con el ícono genérico.
        }
        return SystemIcons.Application;
    }
}
