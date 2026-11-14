using System.ComponentModel;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>The tray icon's menu and tooltip: Open, a Sync now entry per account and Quit (#1409). It follows the overview, so the menu changes when accounts are added, removed or renamed.</summary>
public sealed class TrayMenuModel : LocalizedObservableObject
{
    private readonly SyncOverviewViewModel overview;
    private readonly ISyncActions actions;
    private readonly IMainWindowVisibility window;
    private readonly IAppShutdown shutdown;

    /// <summary>Creates the model and builds the first menu.</summary>
    /// <param name="overview">Supplies the accounts and the overall status.</param>
    /// <param name="actions">Starts a sync for one account.</param>
    /// <param name="window">Shows the main window.</param>
    /// <param name="shutdown">Quits the app.</param>
    /// <param name="localization">Supplies the text.</param>
    public TrayMenuModel(SyncOverviewViewModel overview, ISyncActions actions, IMainWindowVisibility window, IAppShutdown shutdown, ILocalizationService localization) : base(localization)
    {
        this.overview = overview;
        this.actions = actions;
        this.window = window;
        this.shutdown = shutdown;
        Items = Build();
        overview.PropertyChanged += OnOverviewChanged;
    }

    /// <summary>Open, one Sync now entry per account, then Quit.</summary>
    public IReadOnlyList<TrayMenuItemViewModel> Items { get; private set; }

    /// <summary>The app name and the overall status, for example "AStar Dev CloudSync: Up to date".</summary>
    public string TooltipText => Localization.GetLocal("Tray.Tooltip", Title, overview.OverallStatusText());

    private string Title => Localization.GetLocal("App.Title");

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SyncOverviewViewModel.Accounts))
            return;

        Items = Build();
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(TooltipText));
    }

    private TrayMenuItemViewModel[] Build() =>
    [
        new(Localization.GetLocal("Tray.Open", Title), new AsyncRelayCommand(() => ShowWindowAsync())),
        .. overview.Accounts.Select(account => new TrayMenuItemViewModel(Localization.GetLocal("Tray.SyncNow", account.Name), new AsyncRelayCommand(cancellationToken => actions.SyncNowAsync(account.Id, cancellationToken)))),
        new(Localization.GetLocal("Tray.Quit", Title), new AsyncRelayCommand(cancellationToken => shutdown.RequestQuitAsync(cancellationToken))),
    ];

    private Task ShowWindowAsync()
    {
        window.Show();

        return Task.CompletedTask;
    }
}
