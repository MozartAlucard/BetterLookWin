using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BetterLookWin.Contracts;

namespace BetterLookWin.Core;

/// <summary>
/// Icono de bandeja con un interruptor por función (y sus acciones), "Configuración..." y "Salir".
/// </summary>
internal sealed class TrayController : IDisposable
{
    private readonly FeatureHost _host;
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu = new();
    private readonly Dictionary<string, ToolStripMenuItem> _featureItems = new();
    private readonly List<(IFeature Feature, ToolStripMenuItem Item)> _actionItems = new();
    private SettingsWindow? _settingsWindow;
    private ToolStripMenuItem? _startupItem;

    public TrayController(FeatureHost host)
    {
        _host = host;

        DarkMenuRenderer.Apply(_menu);
        BuildMenu();

        _icon = new NotifyIcon
        {
            Icon = AppAssets.TrayIcon(),
            Text = "Better Look Windows",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => OpenSettings();
        _menu.Opening += (_, _) => { if (_startupItem != null) _startupItem.Checked = StartupManager.IsEnabled(); };

        _host.FeatureStateChanged += RefreshChecks;
        _host.FeaturesChanged += BuildMenu;
    }

    private void BuildMenu()
    {
        _menu.Items.Clear();
        _featureItems.Clear();
        _actionItems.Clear();

        _menu.Items.Add(new ToolStripMenuItem("Better Look Windows") { Enabled = false });
        _menu.Items.Add(new ToolStripSeparator());

        foreach (var feature in _host.Features)
        {
            var captured = feature;
            var item = new ToolStripMenuItem(feature.Name) { Checked = _host.IsEnabled(feature) };
            item.Click += (_, _) => _host.SetEnabled(captured, !_host.IsEnabled(captured));
            _featureItems[feature.Id] = item;
            _menu.Items.Add(item);

            foreach (var action in feature.Actions)
            {
                var run = action.Run;
                var actionItem = new ToolStripMenuItem("      " + action.Label)
                {
                    Visible = _host.IsEnabled(feature)
                };
                actionItem.Click += (_, _) => run();
                _actionItems.Add((feature, actionItem));
                _menu.Items.Add(actionItem);
            }
        }

        _menu.Items.Add(new ToolStripSeparator());
        _startupItem = new ToolStripMenuItem("Iniciar con Windows") { Checked = StartupManager.IsEnabled() };
        _startupItem.Click += (_, _) =>
        {
            StartupManager.SetEnabled(!StartupManager.IsEnabled());
            _startupItem.Checked = StartupManager.IsEnabled();
        };
        _menu.Items.Add(_startupItem);
        _menu.Items.Add("Configuración...", null, (_, _) => OpenSettings());
        _menu.Items.Add("Salir", null, (_, _) => System.Windows.Application.Current.Shutdown());

        DarkMenuRenderer.StyleItems(_menu);
    }

    private void RefreshChecks()
    {
        foreach (var feature in _host.Features)
            if (_featureItems.TryGetValue(feature.Id, out var item))
                item.Checked = _host.IsEnabled(feature);

        foreach (var (feature, item) in _actionItems)
            item.Visible = _host.IsEnabled(feature);
    }

    public void OpenSettings()
    {
        if (_settingsWindow != null)
        {
            if (_settingsWindow.WindowState == System.Windows.WindowState.Minimized)
                _settingsWindow.WindowState = System.Windows.WindowState.Normal;
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_host);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void Dispose()
    {
        _host.FeatureStateChanged -= RefreshChecks;
        _host.FeaturesChanged -= BuildMenu;
        _icon.Visible = false;
        _icon.Dispose();
    }
}
