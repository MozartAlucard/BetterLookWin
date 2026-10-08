using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BetterLookWin.Core;

/// <summary>
/// Pantalla de bienvenida: la imagen aparece con fundido, se queda un momento y se desvanece.
/// Sin marco ni fondo; solo la imagen flotando en el centro de la pantalla.
/// </summary>
internal sealed class SplashWindow : Window
{
    private const double SizeDip = 440;
    private static readonly Duration FadeIn = new(TimeSpan.FromMilliseconds(450));
    private static readonly Duration FadeOut = new(TimeSpan.FromMilliseconds(450));
    private static readonly TimeSpan Hold = TimeSpan.FromMilliseconds(1500);

    private readonly ScaleTransform _scale = new(0.94, 0.94);
    private bool _started;

    public SplashWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Width = SizeDip;
        Height = SizeDip;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = AppAssets.WindowIcon();
        Opacity = 0;

        var image = new Image
        {
            Source = AppAssets.Splash(),
            Stretch = Stretch.Uniform,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = _scale
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        Content = image;
    }

    /// <summary>Arranca la animación (se llama cuando todo lo demás ya está cargado). Al terminar, cierra la ventana.</summary>
    public void Begin()
    {
        if (_started) return;
        _started = true;

        var fadeIn = new DoubleAnimation(0, 1, FadeIn) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        var grow = new DoubleAnimation(0.94, 1, new Duration(TimeSpan.FromMilliseconds(900)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);

        fadeIn.Completed += (_, _) =>
        {
            var fadeOut = new DoubleAnimation(1, 0, FadeOut)
            {
                BeginTime = Hold,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        };
        BeginAnimation(OpacityProperty, fadeIn);
    }
}
