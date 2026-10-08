using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;

namespace BetterLookWin.Contracts;

/// <summary>
/// Tema oscuro de BetterLookWin (diseño "Obsidian Utility"). Las funciones lo usan para que sus ventanas
/// y sus paneles de opciones se vean igual que la app:
/// <code>FeatureTheme.Apply(this);   // dentro del constructor de una ventana</code>
/// Si por algún motivo el tema no se puede cargar, la ventana se queda con el estilo normal de Windows.
/// </summary>
public static class FeatureTheme
{
    public const string BackgroundHex = "#0A0A0C";
    public const string SurfaceHex = "#18181E";
    public const string TextHex = "#E4E1E7";
    public const string AccentHex = "#8B5CF6";

    public static readonly FontFamily UiFont = new("Inter, Segoe UI Variable Text, Segoe UI");
    public static readonly FontFamily MonoFont = new("JetBrains Mono, Cascadia Mono, Consolas");

    /// <summary>Estilos oscuros para Button, TextBox, CheckBox, Slider, ComboBox y barras de desplazamiento
    /// (más el estilo con nombre "PrimaryButton"). Devuelve null si no se pudo cargar.</summary>
    public static ResourceDictionary? CreateResources()
    {
        try
        {
            return (ResourceDictionary)XamlReader.Parse(ResourcesXaml);
        }
        catch (Exception ex)
        {
            LogError(ex);
            return null;
        }
    }

    /// <summary>Pinta la ventana con el tema oscuro: colores, fuente, estilos, barra de título oscura e ícono de la app.</summary>
    public static void Apply(Window window)
    {
        window.Background = Solid(BackgroundHex);
        window.Foreground = Solid(TextHex);
        window.FontFamily = UiFont;

        var resources = CreateResources();
        if (resources != null) window.Resources.MergedDictionaries.Add(resources);

        try
        {
            var icon = new System.Windows.Media.Imaging.BitmapImage();
            icon.BeginInit();
            icon.UriSource = new Uri("pack://application:,,,/Assets/icon.png", UriKind.Absolute);
            icon.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            icon.EndInit();
            icon.Freeze();
            window.Icon = icon;
        }
        catch
        {
            // Sin ícono, la ventana sigue funcionando.
        }

        window.SourceInitialized += (_, _) => DarkTitleBar(window);
    }

    private static Brush Solid(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    // ---------- Barra de título oscura (Windows 10 20H1+ / Windows 11) ----------

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static void DarkTitleBar(Window window)
    {
        try
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int dark = 1;
            DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));        // modo oscuro
            int caption = 0x000C0A0A;                                       // #0A0A0C en formato BGR
            DwmSetWindowAttribute(hwnd, 35, ref caption, sizeof(int));     // color de la barra (Windows 11)
            int text = 0x00E7E1E4;                                          // #E4E1E7
            DwmSetWindowAttribute(hwnd, 36, ref text, sizeof(int));         // color del título (Windows 11)
        }
        catch
        {
            // En versiones viejas de Windows no existe: la barra queda con el estilo normal.
        }
    }

    private static void LogError(Exception ex)
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetterLookWin");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "theme-error.txt"), ex.ToString());
        }
        catch
        {
        }
    }

    // ---------- Estilos ----------

    private const string ResourcesXaml = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

          <Style TargetType="Button">
            <Setter Property="Foreground" Value="#E4E1E7"/>
            <Setter Property="Background" Value="#18181E"/>
            <Setter Property="BorderBrush" Value="#14FFFFFF"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="Button">
                  <Border x:Name="bd" Background="{TemplateBinding Background}"
                          BorderBrush="{TemplateBinding BorderBrush}"
                          BorderThickness="{TemplateBinding BorderThickness}"
                          CornerRadius="4" Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                      <Setter TargetName="bd" Property="BorderBrush" Value="#8B5CF6"/>
                    </Trigger>
                    <Trigger Property="IsPressed" Value="True">
                      <Setter TargetName="bd" Property="Opacity" Value="0.85"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                      <Setter Property="Opacity" Value="0.4"/>
                    </Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>

          <Style x:Key="PrimaryButton" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderBrush" Value="#26FFFFFF"/>
            <Setter Property="Background">
              <Setter.Value>
                <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                  <GradientStop Color="#8B5CF6" Offset="0"/>
                  <GradientStop Color="#7C3AED" Offset="1"/>
                </LinearGradientBrush>
              </Setter.Value>
            </Setter>
          </Style>

          <Style TargetType="TextBox">
            <Setter Property="Background" Value="#0A0A0C"/>
            <Setter Property="Foreground" Value="#E4E1E7"/>
            <Setter Property="BorderBrush" Value="#14FFFFFF"/>
            <Setter Property="CaretBrush" Value="#E4E1E7"/>
            <Setter Property="Padding" Value="6,4"/>
          </Style>

          <Style TargetType="CheckBox">
            <Setter Property="Foreground" Value="#E4E1E7"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="CheckBox">
                  <StackPanel Orientation="Horizontal" Background="Transparent">
                    <Border x:Name="box" Width="18" Height="18" CornerRadius="4"
                            Background="#0A0A0C" BorderBrush="#33FFFFFF" BorderThickness="1"
                            VerticalAlignment="Center">
                      <Path x:Name="tick" Data="M3,8 L7,12 L13,4" Stroke="White" StrokeThickness="2"
                            StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round"
                            Visibility="Collapsed"/>
                    </Border>
                    <ContentPresenter Margin="8,0,0,0" VerticalAlignment="Center"/>
                  </StackPanel>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsChecked" Value="True">
                      <Setter TargetName="box" Property="Background" Value="#7C3AED"/>
                      <Setter TargetName="box" Property="BorderBrush" Value="#8B5CF6"/>
                      <Setter TargetName="tick" Property="Visibility" Value="Visible"/>
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="True">
                      <Setter TargetName="box" Property="BorderBrush" Value="#8B5CF6"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                      <Setter Property="Opacity" Value="0.4"/>
                    </Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>

          <Style TargetType="Slider">
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="Slider">
                  <Grid Height="22" Background="Transparent">
                    <Border Height="4" CornerRadius="2" Background="#26FFFFFF" VerticalAlignment="Center"/>
                    <Track x:Name="PART_Track">
                      <Track.DecreaseRepeatButton>
                        <RepeatButton Command="Slider.DecreaseLarge" Focusable="False" IsTabStop="False">
                          <RepeatButton.Template>
                            <ControlTemplate TargetType="RepeatButton">
                              <Border Background="Transparent" Height="22">
                                <Border Height="4" CornerRadius="2" Background="#8B5CF6" VerticalAlignment="Center"/>
                              </Border>
                            </ControlTemplate>
                          </RepeatButton.Template>
                        </RepeatButton>
                      </Track.DecreaseRepeatButton>
                      <Track.IncreaseRepeatButton>
                        <RepeatButton Command="Slider.IncreaseLarge" Focusable="False" IsTabStop="False">
                          <RepeatButton.Template>
                            <ControlTemplate TargetType="RepeatButton">
                              <Border Background="Transparent" Height="22"/>
                            </ControlTemplate>
                          </RepeatButton.Template>
                        </RepeatButton>
                      </Track.IncreaseRepeatButton>
                      <Track.Thumb>
                        <Thumb Width="14" Height="14" Focusable="False">
                          <Thumb.Template>
                            <ControlTemplate TargetType="Thumb">
                              <Ellipse Fill="#E4E1E7" Stroke="#8B5CF6" StrokeThickness="2"/>
                            </ControlTemplate>
                          </Thumb.Template>
                        </Thumb>
                      </Track.Thumb>
                    </Track>
                  </Grid>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>

          <Style TargetType="ComboBoxItem">
            <Setter Property="Foreground" Value="#E4E1E7"/>
            <Setter Property="Padding" Value="10,6"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="ComboBoxItem">
                  <Border x:Name="bd" Background="Transparent" Padding="{TemplateBinding Padding}">
                    <ContentPresenter/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsHighlighted" Value="True">
                      <Setter TargetName="bd" Property="Background" Value="#228B5CF6"/>
                    </Trigger>
                    <Trigger Property="IsSelected" Value="True">
                      <Setter TargetName="bd" Property="Background" Value="#448B5CF6"/>
                    </Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>

          <Style TargetType="ComboBox">
            <Setter Property="Foreground" Value="#E4E1E7"/>
            <Setter Property="Padding" Value="10,6"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="ComboBox">
                  <Grid>
                    <ToggleButton x:Name="toggle" Focusable="False" ClickMode="Press"
                                  IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}">
                      <ToggleButton.Template>
                        <ControlTemplate TargetType="ToggleButton">
                          <Border x:Name="bd" Background="#18181E" BorderBrush="#14FFFFFF"
                                  BorderThickness="1" CornerRadius="4">
                            <Path Data="M0,0 L4,4 L8,0" Stroke="#A1A1AA" StrokeThickness="1.5"
                                  HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,10,0"/>
                          </Border>
                          <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                              <Setter TargetName="bd" Property="BorderBrush" Value="#8B5CF6"/>
                            </Trigger>
                          </ControlTemplate.Triggers>
                        </ControlTemplate>
                      </ToggleButton.Template>
                    </ToggleButton>
                    <ContentPresenter IsHitTestVisible="False" Margin="{TemplateBinding Padding}"
                                      Content="{TemplateBinding SelectionBoxItem}"
                                      ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                      HorizontalAlignment="Left" VerticalAlignment="Center"/>
                    <Popup x:Name="PART_Popup" Placement="Bottom" Focusable="False" AllowsTransparency="True"
                           PopupAnimation="Fade"
                           IsOpen="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}">
                      <Border MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}"
                              MaxHeight="{TemplateBinding MaxDropDownHeight}"
                              Margin="0,4,0,0" Background="#18181E" BorderBrush="#26FFFFFF"
                              BorderThickness="1" CornerRadius="4">
                        <ScrollViewer>
                          <ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/>
                        </ScrollViewer>
                      </Border>
                    </Popup>
                  </Grid>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>

          <Style TargetType="ScrollBar">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="ScrollBar">
                  <Grid x:Name="root" Width="10" Background="Transparent">
                    <Track x:Name="PART_Track" IsDirectionReversed="True">
                      <Track.DecreaseRepeatButton>
                        <RepeatButton Command="ScrollBar.PageUpCommand" Opacity="0" Focusable="False" IsTabStop="False"/>
                      </Track.DecreaseRepeatButton>
                      <Track.IncreaseRepeatButton>
                        <RepeatButton Command="ScrollBar.PageDownCommand" Opacity="0" Focusable="False" IsTabStop="False"/>
                      </Track.IncreaseRepeatButton>
                      <Track.Thumb>
                        <Thumb>
                          <Thumb.Template>
                            <ControlTemplate TargetType="Thumb">
                              <Border x:Name="t" Margin="2" CornerRadius="3" Background="#33FFFFFF"/>
                              <ControlTemplate.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                  <Setter TargetName="t" Property="Background" Value="#668B5CF6"/>
                                </Trigger>
                              </ControlTemplate.Triggers>
                            </ControlTemplate>
                          </Thumb.Template>
                        </Thumb>
                      </Track.Thumb>
                    </Track>
                  </Grid>
                  <ControlTemplate.Triggers>
                    <Trigger Property="Orientation" Value="Horizontal">
                      <Setter TargetName="root" Property="Width" Value="Auto"/>
                      <Setter TargetName="root" Property="Height" Value="10"/>
                      <Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False"/>
                    </Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>

        </ResourceDictionary>
        """;
}
