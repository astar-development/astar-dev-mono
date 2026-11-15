using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Tray;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AStarDev.CloudSync.Tray;

/// <summary>Shows the tray icon with its menu and tooltip, and keeps both in step with <see cref="TrayMenuModel"/> (#1409).</summary>
/// <param name="model">The menu and tooltip.</param>
/// <param name="window">Shows the main window when the icon is clicked.</param>
[ExcludeFromCodeCoverage(Justification = "The platform tray cannot be driven from tests; the menu model is tested.")]
internal sealed class TrayIconService(TrayMenuModel model, IMainWindowVisibility window) : IDisposable
{
    private const string IconFolder = "avares://AStarDev.CloudSync/Assets/";

    private readonly Dictionary<string, WindowIcon> icons = [];
    private TrayIcon? icon;

    /// <summary>Creates the tray icon and starts following the model.</summary>
    /// <param name="application">The application that owns the icon.</param>
    internal void Start(Application application)
    {
        icon = new TrayIcon { Icon = IconFor(model.IconState), ToolTipText = model.TooltipText, Menu = BuildMenu(), IsVisible = true };
        icon.Clicked += (_, _) => window.Show();
        TrayIcon.SetIcons(application, [icon]);
        model.PropertyChanged += OnModelChanged;
    }

    /// <summary>Removes the icon.</summary>
    public void Dispose()
    {
        model.PropertyChanged -= OnModelChanged;
        icon?.Dispose();
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e) => Dispatcher.UIThread.Post(Refresh);

    private void Refresh()
    {
        if (icon is null)
            return;

        icon.ToolTipText = model.TooltipText;
        icon.Icon = IconFor(model.IconState);
        icon.Menu = BuildMenu();
    }

    private static string IconFile(TrayIconState state) => state switch
    {
        TrayIconState.Syncing => "tray-syncing.ico",
        TrayIconState.Attention => "tray-attention.ico",
        TrayIconState.Faulted => "tray-faulted.ico",
        _ => "astar.ico",
    };

    private WindowIcon IconFor(TrayIconState state)
    {
        var file = IconFile(state);
        if (!icons.TryGetValue(file, out var cached))
            icons[file] = cached = new WindowIcon(AssetLoader.Open(new Uri($"{IconFolder}{file}")));

        return cached;
    }

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();
        foreach (var item in model.Items)
        {
            var entry = new NativeMenuItem(item.Text);
            entry.Click += (_, _) => item.Command.Execute(null);
            menu.Items.Add(entry);
        }

        return menu;
    }
}
