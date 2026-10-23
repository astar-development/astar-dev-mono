using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Owns the system tray icon, keeps its menu in step with the accounts, and hides the main window to the tray instead of closing it.</summary>
[ExcludeFromCodeCoverage]
public sealed class TrayIconService(IShutdownCoordinator shutdownCoordinator, ITrayMenuBuilder menuBuilder, AccountsViewModel accountsViewModel, ISyncScheduler syncScheduler, ILocalizationService loc) : IDisposable
{
    private TrayIcon? trayIcon;
    private Window? mainWindow;
    private IClassicDesktopStyleApplicationLifetime? desktop;

    /// <summary>Shows the tray icon and makes closing <paramref name="window"/> hide it to the tray.</summary>
    public void Attach(IClassicDesktopStyleApplicationLifetime lifetime, Window window)
    {
        desktop = lifetime;
        mainWindow = window;
        window.Closing += OnMainWindowClosing;

        trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AStarDev.OneDriveSyncClient/Assets/astar.png"))),
            ToolTipText = loc.GetLocal("Tray.Tooltip"),
            Menu = BuildMenu(),
            IsVisible = true
        };
        trayIcon.Clicked += (_, _) => ShowMainWindow();
        accountsViewModel.Accounts.CollectionChanged += OnAccountsChanged;
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is not WindowCloseReason.WindowClosing)
            return;

        if (shutdownCoordinator.DecideOnWindowClose() is not CloseAction.HideToTray)
            return;

        e.Cancel = true;
        mainWindow?.Hide();
    }

    private void OnAccountsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (trayIcon is not null)
            trayIcon.Menu = BuildMenu();
    }

    private NativeMenu BuildMenu()
    {
        var accounts = accountsViewModel.Accounts.Select(card => TrayAccountFactory.Create(card.Id, card.DisplayName)).ToList();
        var menu = new NativeMenu();

        foreach (var entry in menuBuilder.Build(accounts))
            menu.Items.Add(CreateItem(entry));

        return menu;
    }

    private NativeMenuItem CreateItem(TrayMenuEntry entry)
    {
        var item = new NativeMenuItem(entry switch
        {
            TrayMenuEntry.Open open => open.Label,
            TrayMenuEntry.SyncNow syncNow => syncNow.Label,
            TrayMenuEntry.Quit quit => quit.Label,
            _ => string.Empty
        });
        item.Click += (_, _) => Handle(entry);

        return item;
    }

    private void Handle(TrayMenuEntry entry)
    {
        switch (entry)
        {
            case TrayMenuEntry.Open:
                ShowMainWindow();
                break;
            case TrayMenuEntry.SyncNow syncNow:
                _ = syncScheduler.TriggerAccountAsync(syncNow.AccountId);
                break;
            case TrayMenuEntry.Quit:
                shutdownCoordinator.RequestQuit();
                Dispose();
                desktop?.Shutdown();
                break;
        }
    }

    private void ShowMainWindow()
    {
        mainWindow?.Show();
        mainWindow?.Activate();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        trayIcon?.Dispose();
        trayIcon = null;
    }
}
