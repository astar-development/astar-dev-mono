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

    /// <summary>The app name and the overall status with counts, for example "AStar Dev CloudSync: Syncing 2 accounts" or "AStar Dev CloudSync: 3 items need attention".</summary>
    public string TooltipText => Localization.GetLocal("Tray.Tooltip", Title, StatusText());

    /// <summary>The icon to show: an error beats a sign-in, which beats syncing, which beats conflicts, which beats idle. It follows the tooltip.</summary>
    public TrayIconState IconState => overview.Accounts switch
    {
        var accounts when accounts.Any(account => account.Status.IsFailed) => TrayIconStateFactory.CreateFaulted(),
        var accounts when accounts.Any(account => account.NeedsSignIn) => TrayIconStateFactory.CreateAttention(),
        var accounts when accounts.Any(account => account.Status.IsSyncing) => TrayIconStateFactory.CreateSyncing(),
        _ => overview.ConflictCount > 0 ? TrayIconStateFactory.CreateAttention() : TrayIconStateFactory.CreateIdle(),
    };

    private string Title => Localization.GetLocal("App.Title");

    private string StatusText()
    {
        if (overview.Accounts.Any(account => account.NeedsSignIn))
            return overview.OverallStatusText();

        var syncing = overview.Accounts.Count(account => account.Status.IsSyncing);
        if (syncing > 0)
            return Localization.GetLocal(CountKey.For("Tray.Status.Syncing", syncing), syncing.ToString("N0", Localization.CurrentCulture));

        return overview.ConflictCount > 0
            ? Localization.GetLocal(CountKey.For("Tray.Status.Attention", overview.ConflictCount), overview.ConflictCount.ToString("N0", Localization.CurrentCulture))
            : overview.OverallStatusText();
    }

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SyncOverviewViewModel.Accounts))
            return;

        Items = Build();
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(TooltipText));
        OnPropertyChanged(nameof(IconState));
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
