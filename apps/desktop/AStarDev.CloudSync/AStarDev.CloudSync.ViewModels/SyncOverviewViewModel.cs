using AStarDev.CloudSync.Core;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>The data every main window layout binds to: accounts, sync pairs, activity, conflicts, summary figures and throughput.</summary>
/// <param name="source">Supplies the overview data.</param>
/// <param name="localization">Supplies the text and culture.</param>
/// <param name="timeProvider">Supplies the current time for relative times.</param>
/// <param name="logger">Reports an overview that cannot be read.</param>
public sealed class SyncOverviewViewModel(ISyncOverviewSource source, ILocalizationService localization, TimeProvider timeProvider, ILogger<SyncOverviewViewModel> logger) : LocalizedObservableObject(localization)
{
    private SyncOverview overview = SyncOverviewFactory.Create([], [], [], [], []);

    /// <summary>The connected accounts.</summary>
    public IReadOnlyList<AccountViewModel> Accounts { get; private set; } = [];

    /// <summary>The sync pairs across every account.</summary>
    public IReadOnlyList<SyncPairViewModel> SyncPairs { get; private set; } = [];

    /// <summary>Recent activity across every account, newest first.</summary>
    public IReadOnlyList<ActivityItemViewModel> Activity { get; private set; } = [];

    /// <summary>Conflicts waiting for the user.</summary>
    public IReadOnlyList<ConflictViewModel> Conflicts { get; private set; } = [];

    /// <summary>The summary figures: accounts, providers, folders synced, conflicts and overall status.</summary>
    public IReadOnlyList<StatTileViewModel> Stats { get; private set; } = [];

    /// <summary>Recent transfer rates.</summary>
    public ThroughputViewModel Throughput { get; private set; } = new([], localization);

    /// <summary>How far the overview load has got.</summary>
    public SyncOverviewLoadState LoadState { get; private set; } = SyncOverviewLoadStateFactory.CreateNotLoaded();

    /// <summary>The text explaining that the overview could not be read; empty unless the load failed.</summary>
    public string LoadErrorText => LoadState is SyncOverviewLoadState.Failed ? Localization.GetLocal("Overview.LoadFailed") : string.Empty;

    /// <summary>Reads the overview from the source and rebuilds every collection. A failing source is logged and reported through <see cref="LoadState"/> instead of throwing; cancellation still throws.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            overview = await source.GetOverviewAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogMessage.Error(logger, $"{nameof(SyncOverviewViewModel)} could not load the sync overview", exception);
            SetLoadState(SyncOverviewLoadStateFactory.CreateFailed());

            return;
        }

        var accountsById = overview.Accounts.ToDictionary(account => account.Id);
        var pairNamesById = overview.SyncPairs.ToDictionary(pair => pair.Id, pair => pair.Name);

        Accounts = [.. overview.Accounts.Select(account => new AccountViewModel(account, overview.Activity, Localization, timeProvider))];
        SyncPairs = [.. overview.SyncPairs.Where(pair => accountsById.ContainsKey(pair.AccountId)).Select(pair => new SyncPairViewModel(pair, accountsById[pair.AccountId], Localization, timeProvider))];
        Activity = [.. overview.Activity.Where(entry => accountsById.ContainsKey(entry.AccountId)).OrderByDescending(entry => entry.OccurredAt).Select(entry => new ActivityItemViewModel(entry, new ProviderViewModel(accountsById[entry.AccountId].Provider, Localization), Localization, timeProvider))];
        Conflicts = [.. overview.Conflicts.Select(conflict => new ConflictViewModel(conflict, pairNamesById.GetValueOrDefault(conflict.SyncPairId, string.Empty), Localization))];
        Stats = CreateStats();
        Throughput = new ThroughputViewModel(overview.Throughput, Localization);

        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(SyncPairs));
        OnPropertyChanged(nameof(Activity));
        OnPropertyChanged(nameof(Conflicts));
        OnPropertyChanged(nameof(Stats));
        OnPropertyChanged(nameof(Throughput));
        SetLoadState(SyncOverviewLoadStateFactory.CreateLoaded());
    }

    private void SetLoadState(SyncOverviewLoadState state)
    {
        LoadState = state;
        OnPropertyChanged(nameof(LoadState));
        OnPropertyChanged(nameof(LoadErrorText));
    }

    private StatTileViewModel[] CreateStats()
    {
        int providerCount = overview.Accounts.Select(account => account.Provider).Distinct().Count();
        int folderCount = overview.Accounts.Sum(account => account.FolderCount);

        return
        [
            new(() => overview.Accounts.Count.ToString("N0", Localization.CurrentCulture), CountKey.For("Stats.Accounts", overview.Accounts.Count), StatTileToneFactory.CreateNormal(), Localization),
            new(() => providerCount.ToString("N0", Localization.CurrentCulture), CountKey.For("Stats.Providers", providerCount), StatTileToneFactory.CreateNormal(), Localization),
            new(() => folderCount.ToString("N0", Localization.CurrentCulture), CountKey.For("Stats.FoldersSynced", folderCount), StatTileToneFactory.CreateNormal(), Localization),
            new(() => overview.Conflicts.Count.ToString("N0", Localization.CurrentCulture), CountKey.For("Stats.Conflicts", overview.Conflicts.Count), overview.Conflicts.Count > 0 ? StatTileToneFactory.CreateWarn() : StatTileToneFactory.CreateNormal(), Localization),
            new(OverallStatusText, "Stats.OverallStatus", StatTileToneFactory.CreateAccent(), Localization)
        ];
    }

    private string OverallStatusText()
    {
        if (overview.Accounts.Any(account => account.Status is SyncStatus.Syncing))
            return Localization.GetLocal("Status.Overall.Syncing");

        if (overview.Conflicts.Count > 0)
            return Localization.GetLocal("Status.Overall.Conflicts");

        if (overview.Accounts.Count > 0 && overview.Accounts.All(account => account.Status is SyncStatus.Paused))
            return Localization.GetLocal("Status.Paused");

        return Localization.GetLocal("Status.UpToDate");
    }
}
