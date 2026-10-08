using System.Windows.Media.Effects;
using BetterLookWin.Contracts;
using BetterLookWin.Packages;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace BetterLookWin.Core;

/// <summary>
/// Pantalla principal: una tarjeta por función (con su interruptor ON/OFF), un botón "+" para el
/// catálogo y una barra de estado. Al pulsar una tarjeta se abren sus opciones.
/// Se construye sola a partir de FeatureHost: una función nueva aparece aquí sin tocar este archivo.
/// </summary>
internal sealed class SettingsWindow : Window
{
    private const string Brand = "#8B5CF6";

    private static readonly FontFamily Mono = new("JetBrains Mono, Cascadia Mono, Consolas");

    private readonly FeatureHost _host;
    private readonly ContentControl _body = new();
    private readonly TextBlock _footerText = new()
    {
        FontFamily = Mono,
        FontSize = 11,
        VerticalAlignment = VerticalAlignment.Center
    };
    private IFeature? _selected;

    private readonly Border _dropOverlay = new()
    {
        Visibility = Visibility.Collapsed,
        IsHitTestVisible = false,
        Background = new SolidColorBrush(Color.FromArgb(215, 8, 8, 10)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)),
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(12),
        Margin = new Thickness(12),
        Child = new TextBlock
        {
            Text = "Suelta el .zip para instalar la función",
            FontSize = 16,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    public SettingsWindow(FeatureHost host)
    {
        _host = host;

        Title = "Better Look Windows";
        Icon = AppAssets.WindowIcon();
        Width = 820;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanMinimize;
        Background = B("#0A0A0C");
        Foreground = B("#E4E1E7");
        FontFamily = new FontFamily("Inter, Segoe UI Variable Text, Segoe UI");

        var theme = FeatureTheme.CreateResources();
        if (theme != null) Resources.MergedDictionaries.Add(theme);
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 44,
            ResizeBorderThickness = new Thickness(0),
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false
        });

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildHeader();
        var footer = BuildFooter();
        Grid.SetRow(header, 0);
        Grid.SetRow(_body, 1);
        Grid.SetRow(footer, 2);
        root.Children.Add(header);
        root.Children.Add(_body);
        root.Children.Add(footer);
        Grid.SetRowSpan(_dropOverlay, 3);
        root.Children.Add(_dropOverlay);
        Content = root;

        AllowDrop = true;
        DragEnter += (_, e) => OnDrag(e);
        DragOver += (_, e) => OnDrag(e);
        DragLeave += (_, _) => _dropOverlay.Visibility = Visibility.Collapsed;
        Drop += OnDropFiles;

        _host.FeatureStateChanged += OnFeatureStateChanged;
        _host.FeaturesChanged += OnFeaturesChanged;
        Closed += (_, _) =>
        {
            _host.FeatureStateChanged -= OnFeatureStateChanged;
            _host.FeaturesChanged -= OnFeaturesChanged;
            _host.Settings.Save();
        };

        ShowHome();
    }

    // ---------- Estructura ----------

    private UIElement BuildHeader()
    {
        var grid = new Grid { Height = 44, Margin = new Thickness(24, 0, 12, 0) };

        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var headerIcon = new Image { Source = AppAssets.WindowIcon(), Width = 20, Height = 20, Margin = new Thickness(0, 0, 10, 0) };
        RenderOptions.SetBitmapScalingMode(headerIcon, BitmapScalingMode.HighQuality);
        left.Children.Add(headerIcon);
        left.Children.Add(new TextBlock
        {
            Text = "Better Look Windows ツ",
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Foreground = B("#CCFFFFFF")
        });
        grid.Children.Add(left);

        var right = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        right.Children.Add(CaptionButton("–", () => WindowState = WindowState.Minimized, danger: false));
        right.Children.Add(CaptionButton("✕", Close, danger: true));
        grid.Children.Add(right);

        return grid;
    }

    private readonly StackPanel _footerRight = new()
    {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center
    };

    private UIElement BuildFooter()
    {
        var grid = new Grid { Margin = new Thickness(24, 10, 24, 10) };
        _footerText.Foreground = B("#71717A");
        grid.Children.Add(_footerText);

        RefreshFooterRight();
        grid.Children.Add(_footerRight);

        return new Border
        {
            BorderBrush = B("#0AFFFFFF"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = grid
        };
    }

    /// <summary>Derecha del pie: interruptor "Iniciar con Windows" y la versión.</summary>
    private void RefreshFooterRight()
    {
        _footerRight.Children.Clear();

        _footerRight.Children.Add(new TextBlock
        {
            Text = "Iniciar con Windows",
            FontFamily = Mono,
            FontSize = 11,
            Foreground = B("#71717A"),
            VerticalAlignment = VerticalAlignment.Center
        });

        var toggle = CreateSwitch(StartupManager.IsEnabled(), value =>
        {
            if (!StartupManager.SetEnabled(value))
            {
                MessageBox.Show(this, "No se pudo cambiar el inicio con Windows.", "Better Look Windows",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            RefreshFooterRight();
        });
        toggle.Margin = new Thickness(10, 0, 18, 0);
        _footerRight.Children.Add(toggle);

        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        _footerRight.Children.Add(new TextBlock
        {
            Text = version != null ? $"v{version.ToString(3)}" : "",
            FontFamily = Mono,
            FontSize = 11,
            Foreground = B("#52525B"),
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private void UpdateFooter()
    {
        var active = _host.Features.Where(f => _host.IsEnabled(f)).Select(f => f.Name).ToList();
        string count = active.Count == 1 ? "1 función activa" : $"{active.Count} funciones activas";
        _footerText.Text = active.Count > 0 ? $"{count}  •  {string.Join(", ", active)}" : count;
    }

    private void OnFeatureStateChanged()
    {
        if (_selected != null) ShowDetails(_selected);
        else ShowHome();
    }

    // ---------- Pantalla principal ----------

    private void ShowHome()
    {
        _selected = null;

        var wrap = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var feature in _host.Features) wrap.Children.Add(BuildCard(feature));
        wrap.Children.Add(BuildAddTile());

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24, 12, 24, 24)
        };
        stack.Children.Add(wrap);
        stack.Children.Add(new TextBlock
        {
            Text = "pulsa + o arrastra un .zip aquí para instalar una función",
            FontFamily = Mono,
            FontSize = 12,
            Foreground = B("#52525B"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        });
        stack.Children.Add(BuildFolderRow());

        // El contenido se centra en la ventana y, si no cabe, aparece la barra de desplazamiento.
        var host = new Grid();
        host.Children.Add(stack);
        var scroll = new ScrollViewer { Content = host, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.SizeChanged += (_, e) => host.MinHeight = e.NewSize.Height;
        _body.Content = scroll;
        UpdateFooter();
    }

    // ---------- Carpeta de funciones ----------

    private UIElement BuildFolderRow()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 22, 0, 0)
        };

        row.Children.Add(new TextBlock
        {
            Text = "Carpeta de funciones:",
            FontFamily = Mono,
            FontSize = 11,
            Foreground = B("#71717A"),
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(new TextBlock
        {
            Text = _host.FeaturesFolder,
            ToolTip = _host.FeaturesFolder,
            FontFamily = Mono,
            FontSize = 11,
            Foreground = B("#A1A1AA"),
            MaxWidth = 320,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(8, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        var change = SmallButton("Cambiar…", ChangeFeaturesFolder);
        var open = SmallButton("Abrir", OpenFeaturesFolder);
        open.Margin = new Thickness(6, 0, 0, 0);
        row.Children.Add(change);
        row.Children.Add(open);
        return row;
    }

    private static Button SmallButton(string text, Action click)
    {
        var button = new Button { Content = text, Padding = new Thickness(10, 4, 10, 4), FontSize = 11 };
        button.Click += (_, _) => click();
        return button;
    }

    private void OpenFeaturesFolder()
    {
        try
        {
            string folder = _host.FeaturesFolder;
            System.IO.Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir la carpeta:\n\n" + ex.Message, "Better Look Windows",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ChangeFeaturesFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Elige dónde quieres la carpeta de funciones",
            InitialDirectory = System.IO.Path.GetDirectoryName(_host.FeaturesFolder) ?? ""
        };
        if (dialog.ShowDialog(this) != true) return;

        bool hasInstalled = _host.Features.Any(f => _host.FindPackage(f.Id) != null);
        bool move = false;
        if (hasInstalled)
        {
            var answer = MessageBox.Show(this,
                $"Se creará la carpeta «features» dentro de:\n{dialog.FolderName}\n\n" +
                "¿Mover ahí las funciones que ya tienes instaladas?\n\n" +
                "Sí: se mueven (recomendado)\nNo: se quedan donde están y esta carpeta empieza vacía",
                "Better Look Windows", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return;
            move = answer == MessageBoxResult.Yes;
        }

        var result = _host.ChangeFeaturesFolder(dialog.FolderName, move);
        MessageBox.Show(this, result.Message, "Better Look Windows", MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        ShowHome();
    }

    private UIElement BuildCard(IFeature feature)
    {
        bool on = _host.IsEnabled(feature);

        var card = new Border
        {
            Width = 192,
            Height = 192,
            CornerRadius = new CornerRadius(8),
            Background = B("#121216"),
            BorderBrush = B("#14FFFFFF"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(10),
            Cursor = Cursors.Hand
        };
        card.MouseEnter += (_, _) => card.BorderBrush = B("#808B5CF6");
        card.MouseLeave += (_, _) => card.BorderBrush = B("#14FFFFFF");
        card.MouseLeftButtonUp += (_, _) => ShowDetails(feature);

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Fila superior: etiqueta ON/OFF e interruptor.
        var top = new Grid();
        top.Children.Add(BuildBadge(on));
        var toggle = CreateSwitch(on, value => _host.SetEnabled(feature, value));
        toggle.HorizontalAlignment = HorizontalAlignment.Right;
        top.Children.Add(toggle);
        grid.Children.Add(top);

        // Centro: icono, nombre e identificador.
        var center = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var iconBox = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(8),
            Background = B("#18181E"),
            BorderBrush = B("#14FFFFFF"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8),
            Child = BuildIconContent(feature)
        };
        var iconHost = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        iconHost.Children.Add(iconBox);
        if (on)
        {
            iconHost.Children.Add(new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = B(Brand),
                Stroke = B("#121216"),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -4, -4, 0)
            });
        }
        center.Children.Add(iconHost);
        center.Children.Add(new TextBlock
        {
            Text = feature.Name,
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = B("#F5F5F5"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        center.Children.Add(new TextBlock
        {
            Text = feature.Id.Replace('-', '_'),
            FontFamily = Mono,
            FontSize = 10,
            Foreground = B("#71717A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetRow(center, 1);
        grid.Children.Add(center);

        if (on)
        {
            // Resplandor violeta muy difuso para las funciones activas.
            card.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(0x8B, 0x5C, 0xF6),
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = 0.15
            };
        }

        // Brillo fino en el borde superior, como luz cayendo desde arriba.
        var shell = new Grid();
        shell.Children.Add(grid);
        shell.Children.Add(new Border
        {
            Height = 1,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(6, 0, 6, 0),
            Background = B("#0FFFFFFF"),
            IsHitTestVisible = false
        });
        card.Child = shell;
        return card;
    }

    private static UIElement BuildBadge(bool on) => new Border
    {
        Background = B(on ? "#1A8B5CF6" : "#0DFFFFFF"),
        BorderBrush = B(on ? "#40A78BFA" : "#14FFFFFF"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6, 0, 6, 0),
        Height = 20,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock
        {
            Text = on ? "ON" : "OFF",
            FontFamily = Mono,
            FontSize = 10,
            Foreground = B(on ? "#A78BFA" : "#71717A")
        }
    };

    private UIElement BuildAddTile()
    {
        var rect = new Rectangle
        {
            Width = 56,
            Height = 56,
            RadiusX = 8,
            RadiusY = 8,
            Stroke = B("#14FFFFFF"),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = B("#0A0A0C")
        };
        var plus = new TextBlock
        {
            Text = "+",
            FontSize = 26,
            Foreground = B("#71717A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -4, 0, 0)
        };
        var inner = new Grid { Width = 56, Height = 56, VerticalAlignment = VerticalAlignment.Center };
        inner.Children.Add(rect);
        inner.Children.Add(plus);

        var tile = new Grid
        {
            Width = 56,
            Height = 192,
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = "Instalar una función desde un ZIP"
        };
        tile.Children.Add(inner);

        tile.MouseEnter += (_, _) => { rect.Stroke = B(Brand); plus.Foreground = B(Brand); };
        tile.MouseLeave += (_, _) => { rect.Stroke = B("#14FFFFFF"); plus.Foreground = B("#71717A"); };
        tile.MouseLeftButtonUp += (_, _) => PickAndInstall();

        return tile;
    }

    // ---------- Detalle de una función ----------

    private void ShowDetails(IFeature feature)
    {
        _selected = feature;
        bool on = _host.IsEnabled(feature);

        var panel = new StackPanel
        {
            Margin = new Thickness(24, 4, 24, 24),
            MaxWidth = 560,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var back = new Button
        {
            Content = "←  Volver",
            Padding = new Thickness(12, 5, 12, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 18)
        };
        back.Click += (_, _) => ShowHome();
        panel.Children.Add(back);

        panel.Children.Add(new TextBlock
        {
            Text = feature.Name,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = B("#F5F5F5")
        });
        panel.Children.Add(new TextBlock
        {
            Text = feature.Id.Replace('-', '_') + "  •  " + DescribeOrigin(feature),
            FontFamily = Mono,
            FontSize = 11,
            Foreground = B("#71717A"),
            Margin = new Thickness(0, 2, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = feature.Description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = B("#99FFFFFF"),
            Margin = new Thickness(0, 10, 0, 16)
        });

        var switchRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        switchRow.Children.Add(CreateSwitch(on, value => _host.SetEnabled(feature, value)));
        switchRow.Children.Add(new TextBlock
        {
            Text = on ? "Activada" : "Desactivada",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        });
        panel.Children.Add(switchRow);

        foreach (var action in feature.Actions)
        {
            var run = action.Run;
            var button = new Button
            {
                Style = TryFindResource("PrimaryButton") as Style,
                Content = action.Label,
                Padding = new Thickness(14, 7, 14, 7),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 12),
                IsEnabled = on
            };
            button.Click += (_, _) => run();
            panel.Children.Add(button);
        }

        var options = feature.CreateOptionsView();
        if (options != null)
        {
            panel.Children.Add(new Border
            {
                Background = B("#121216"),
                BorderBrush = B("#14FFFFFF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 4, 0, 0),
                Child = options
            });
        }

        if (_host.FindPackage(feature.Id) != null)
        {
            var remove = new Button
            {
                Content = "Desinstalar función",
                Padding = new Thickness(14, 7, 14, 7),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 20, 0, 0),
                Foreground = B("#FFB4AB")
            };
            remove.Click += (_, _) => UninstallFeature(feature);
            panel.Children.Add(remove);
        }

        _body.Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UpdateFooter();
    }

    // ---------- Instalar funciones desde ZIP ----------

    private void OnFeaturesChanged()
    {
        if (_selected != null && !_host.Features.Contains(_selected)) _selected = null;
        OnFeatureStateChanged();
    }

    private static void OnDrag(DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDropFiles(object sender, DragEventArgs e)
    {
        _dropOverlay.Visibility = Visibility.Collapsed;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        e.Handled = true;

        // Se instala fuera del evento de soltar, para no dejar congelado al Explorador.
        Dispatcher.BeginInvoke(new Action(() => InstallMany(files)));
    }

    private void PickAndInstall()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Elige el ZIP de la función",
            Filter = "Funciones de BetterLookWin (*.zip)|*.zip|Todos los archivos|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true) InstallMany(dialog.FileNames);
    }

    private void InstallMany(string[] files)
    {
        var others = files.Where(f => !f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (string zip in files.Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            InstallFromZip(zip);

        if (others.Count > 0)
        {
            MessageBox.Show(this,
                "Solo se pueden instalar archivos .zip. No admitido: " +
                string.Join(", ", others.Select(f => System.IO.Path.GetFileName(f))),
                "Archivo no admitido", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void InstallFromZip(string zipPath)
    {
        var inspection = PackageStore.Inspect(zipPath);
        if (!inspection.IsValid)
        {
            MessageBox.Show(this,
                "Este ZIP no es una función admitida de BetterLookWin.\n\n" + inspection.Error,
                "ZIP no admitido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var manifest = inspection.Manifest!;
        var existing = _host.FindPackage(manifest.Id);

        string action;
        if (existing == null) action = "¿Instalar esta función?";
        else if (existing.Manifest.Version == manifest.Version) action = $"Ya está instalada la versión {manifest.Version}. ¿Reinstalarla?";
        else action = $"Ya tienes la versión {existing.Manifest.Version}. ¿Cambiar a la {manifest.Version}?";

        string author = string.IsNullOrWhiteSpace(manifest.Author) ? "autor sin indicar" : manifest.Author;
        string text =
            $"{manifest.Name}  (v{manifest.Version})\npor {author}\n\n" +
            (string.IsNullOrWhiteSpace(manifest.Description) ? "" : manifest.Description + "\n\n") +
            action + "\n\n" +
            "Las funciones corren con tus mismos permisos en este equipo. Instala solo funciones de fuentes en las que confíes.";

        if (MessageBox.Show(this, text, "Instalar función", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = _host.Install(zipPath, inspection);
        MessageBox.Show(this, result.Message, "Better Look Windows", MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void UninstallFeature(IFeature feature)
    {
        var answer = MessageBox.Show(this,
            $"¿Desinstalar \"{feature.Name}\"? Se borran sus archivos. Tus ajustes se conservan por si la vuelves a instalar.",
            "Desinstalar función", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        _selected = null;
        var result = _host.Uninstall(feature);
        if (!result.Success)
            MessageBox.Show(this, result.Message, "Better Look Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
        ShowHome();
    }

    private string DescribeOrigin(IFeature feature)
    {
        var package = _host.FindPackage(feature.Id);
        if (package == null) return "integrada";

        string author = package.Manifest.Author;
        return string.IsNullOrWhiteSpace(author)
            ? $"v{package.Manifest.Version}"
            : $"v{package.Manifest.Version}  •  {author}";
    }

    private UIElement BuildIconContent(IFeature feature)
    {
        var image = _host.FindPackage(feature.Id)?.LoadIcon();
        if (image != null)
        {
            return new Image
            {
                Source = image,
                Width = 28,
                Height = 28,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(IconData(feature.Id)),
            Stroke = B(Brand),
            StrokeThickness = 1.6,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = 28,
            Height = 28,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    // ---------- Piezas reutilizables ----------

    private static Border CreateSwitch(bool on, Action<bool> changed)
    {
        var knob = new Ellipse
        {
            Width = 16,
            Height = 16,
            Fill = Brushes.White,
            Margin = new Thickness(1),
            HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        var track = new Border
        {
            Width = 36,
            Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = B(on ? "#7C3AED" : "#18181E"),
            BorderBrush = B(on ? "#7C3AED" : "#1AFFFFFF"),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = knob
        };
        track.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;   // que no cuente como clic en la tarjeta
            changed(!on);
        };
        return track;
    }

    private static UIElement CaptionButton(string glyph, Action click, bool danger)
    {
        var text = new TextBlock
        {
            Text = glyph,
            FontSize = 13,
            Foreground = B("#71717A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var box = new Border
        {
            Width = 40,
            Height = 32,
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = text
        };
        box.MouseEnter += (_, _) => text.Foreground = B(danger ? "#FFB4AB" : "#E4E1E7");
        box.MouseLeave += (_, _) => text.Foreground = B("#71717A");
        box.MouseLeftButtonUp += (_, _) => click();
        WindowChrome.SetIsHitTestVisibleInChrome(box, true);
        return box;
    }

    private static string IconData(string id) => id switch
    {
        // Imagen: marco, sol y montaña.
        "hover-preview" => "M5,3 H19 A2,2 0 0 1 21,5 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 V5 A2,2 0 0 1 5,3 Z M8.5,10 A1.5,1.5 0 1 0 8.5,7 A1.5,1.5 0 1 0 8.5,10 Z M21,15 L16,10 L5,21",
        // Lápiz.
        "icon-editor" => "M12,20 H21 M16.5,3.5 A2.1,2.1 0 0 1 19.5,6.5 L7,19 L3,20 L4,16 Z",
        _ => "M4,4 H20 V20 H4 Z"
    };

    private static Brush B(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}
