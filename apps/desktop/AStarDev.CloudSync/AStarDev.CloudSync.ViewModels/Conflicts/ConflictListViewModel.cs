using System.Collections.Concurrent;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>The attention-queue screen (#1608): the open conflicts grouped by account, filtered by kind, resolved one at a time.</summary>
public sealed class ConflictListViewModel : LocalizedObservableObject
{
    private readonly ConflictListSources sources;
    private readonly IConflictResolver resolver;
    private readonly IHeldDeletionDecider heldDecider;
    private readonly IConflictResolutionPlanner planner;
    private readonly ILogger<ConflictListViewModel> logger;
    private readonly ConcurrentDictionary<ConflictId, string> errorKeys = new();
    private readonly ConcurrentDictionary<ConflictId, byte> skipped = new();
    private int loadsStarted;
    private bool isOpen;
    private IReadOnlyList<AccountItems> accounts = [];
    private ConflictKindFilterViewModel selectedFilter;

    /// <summary>Creates the screen.</summary>
    /// <param name="sources">Supplies the accounts, their open items and their sync pairs.</param>
    /// <param name="resolver">Resolves a conflict.</param>
    /// <param name="heldDecider">Confirms or restores held deletions.</param>
    /// <param name="planner">Says which strategies apply to a conflict.</param>
    /// <param name="localization">Supplies the text.</param>
    /// <param name="logger">Reports reads and resolutions that failed, without paths.</param>
    public ConflictListViewModel(ConflictListSources sources, IConflictResolver resolver, IHeldDeletionDecider heldDecider, IConflictResolutionPlanner planner, ILocalizationService localization, ILogger<ConflictListViewModel> logger) : base(localization)
    {
        this.sources = sources;
        this.resolver = resolver;
        this.heldDecider = heldDecider;
        this.planner = planner;
        this.logger = logger;
        Filters =
        [
            new ConflictKindFilterViewModel(Option.None<ConflictKind>(), localization),
            .. new ConflictKind[]
            {
                ConflictKindFactory.CreateBothModified(),
                ConflictKindFactory.CreateLocalModifiedRemoteDeleted(),
                ConflictKindFactory.CreateLocalDeletedRemoteModified(),
                ConflictKindFactory.CreateBothAdded(),
                ConflictKindFactory.CreateMoveTargetTaken(),
                ConflictKindFactory.CreateRemoteChangedDuringTransfer()
            }.Select(kind => new ConflictKindFilterViewModel(Option.Some(kind), localization))
        ];
        selectedFilter = Filters[0];
    }

    /// <summary>Whether the screen is showing.</summary>
    public bool IsOpen => isOpen;

    /// <summary>How far the read has got.</summary>
    public SyncOverviewLoadState LoadState { get; private set; } = SyncOverviewLoadStateFactory.CreateNotLoaded();

    /// <summary>The conflicts that pass the filter, grouped by account.</summary>
    public IReadOnlyList<ConflictAccountGroupViewModel> Groups { get; private set; } = [];

    /// <summary>Every kind, then each kind.</summary>
    public IReadOnlyList<ConflictKindFilterViewModel> Filters { get; }

    /// <summary>The kind shown.</summary>
    public ConflictKindFilterViewModel SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (ReferenceEquals(selectedFilter, value))
                return;

            selectedFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    /// <summary>Whether the read succeeded and nothing is listed.</summary>
    public bool IsEmpty => LoadState is SyncOverviewLoadState.Loaded && Groups.Count == 0;

    /// <summary>The text shown when nothing is listed.</summary>
    public string EmptyText => Localization.GetLocal("Conflict.Empty");

    /// <summary>The text explaining that the queue could not be read; empty unless the read failed.</summary>
    public string LoadErrorText => LoadState is SyncOverviewLoadState.Failed ? Localization.GetLocal("Conflict.LoadFailed") : string.Empty;

    /// <summary>The screen heading.</summary>
    public string TitleText => Localization.GetLocal("Conflict.Title");

    /// <summary>The text of the button that opens the screen.</summary>
    public string ReviewText => Localization.GetLocal("Conflict.Review");

    /// <summary>The text of the close button.</summary>
    public string CloseText => Localization.GetLocal("Conflict.Close");

    /// <summary>The text of the refresh button.</summary>
    public string RefreshText => Localization.GetLocal("Conflict.Refresh");

    /// <summary>The label of the kind filter.</summary>
    public string FilterLabelText => Localization.GetLocal("Conflict.FilterLabel");

    /// <summary>The held deletion sets, shown when every kind is shown.</summary>
    public IReadOnlyList<HeldDeletionItemViewModel> HeldDeletions => throw new NotImplementedException($"{heldDecider}");

    /// <summary>How many listed conflicts are ticked.</summary>
    public int SelectedCount => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The ticked count in words.</summary>
    public string SelectedText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The text of the Select all button.</summary>
    public string SelectAllText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The text of the Clear selection button.</summary>
    public string ClearSelectionText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The label of the bulk strategy buttons.</summary>
    public string BulkLabelText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The heading of the held deletions section.</summary>
    public string HeldTitleText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The error of the last bulk apply; empty when there is none.</summary>
    public string BulkErrorText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>Ticks every listed conflict.</summary>
    public IRelayCommand SelectAllCommand => throw new NotImplementedException($"{heldDecider}");

    /// <summary>Unticks every conflict.</summary>
    public IRelayCommand ClearSelectionCommand => throw new NotImplementedException($"{heldDecider}");

    /// <summary>One button per strategy; each applies its strategy to the ticked conflicts.</summary>
    public IReadOnlyList<ConflictBulkOptionViewModel> BulkOptions => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The default strategy of the shown kind, or none when every kind is shown.</summary>
    public Option<ConflictStrategy> DefaultStrategy => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The text of the button that applies the default strategy.</summary>
    public string DefaultText => throw new NotImplementedException($"{heldDecider}");

    /// <summary>The accessible name of the default strategy button.</summary>
    public string DefaultAutomationName => throw new NotImplementedException($"{heldDecider}");

    /// <summary>Applies the default strategy of the shown kind to the ticked conflicts.</summary>
    public IAsyncRelayCommand DefaultCommand => throw new NotImplementedException($"{heldDecider}");

    /// <summary>Shows the screen and reads the queue.</summary>
    public IAsyncRelayCommand OpenCommand => field ??= new AsyncRelayCommand(OpenAsync);

    /// <summary>Hides the screen.</summary>
    public IRelayCommand CloseCommand => field ??= new RelayCommand(Close);

    /// <summary>Reads the queue again.</summary>
    public IAsyncRelayCommand RefreshCommand => field ??= new AsyncRelayCommand(LoadAsync);

    /// <summary>Reads every account's open conflicts. A failed read is logged and shown as such, never as an empty queue; cancellation still throws.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var load = Interlocked.Increment(ref loadsStarted);
        _ = await RunAsync(() => ReadAsync(cancellationToken), cancellationToken)
            .MatchAsync(result => IsLatest(load) ? result.Match(Show, NotRead) : Unit.Instance, exception => IsLatest(load) ? ReadThrew(exception) : Unit.Instance);
    }

    private Task OpenAsync(CancellationToken cancellationToken)
    {
        isOpen = true;
        OnPropertyChanged(nameof(IsOpen));

        return LoadAsync(cancellationToken);
    }

    private void Close()
    {
        isOpen = false;
        OnPropertyChanged(nameof(IsOpen));
    }

    private static async Task<Exceptional<T>> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var attempt = Try.RunAsync(operation, cancellationToken);
        await ((Task)attempt).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return attempt.IsCanceled && !cancellationToken.IsCancellationRequested ? new Failure<T>(new TimeoutException()) : await attempt;
    }

    private bool IsLatest(int load) => load == Volatile.Read(ref loadsStarted);

    private Task<Result<IReadOnlyList<AccountQueue>, ErrorResponse>> ReadAsync(CancellationToken cancellationToken) =>
        sources.Overview.GetOverviewAsync(cancellationToken).BindAsync(overview => ReadQueuesAsync(overview.Accounts, cancellationToken));

    private async Task<Result<IReadOnlyList<AccountQueue>, ErrorResponse>> ReadQueuesAsync(IReadOnlyList<CloudAccount> cloudAccounts, CancellationToken cancellationToken)
    {
        List<AccountQueue> queues = [];
        var failure = Option.None<ErrorResponse>();

        foreach (var account in cloudAccounts)
        {
            failure = await ReadAccountAsync(queues, account, cancellationToken);

            if (failure.Match(_ => true, () => false))
                break;
        }

        return failure.Match(error => Result.Failure<IReadOnlyList<AccountQueue>, ErrorResponse>(error), () => Result.Success<IReadOnlyList<AccountQueue>, ErrorResponse>(queues));
    }

    private Task<Option<ErrorResponse>> ReadAccountAsync(List<AccountQueue> queues, CloudAccount account, CancellationToken cancellationToken) =>
        sources.Attention.GetOpenAsync(account.Id, cancellationToken)
            .MatchAsync(
                items =>
                {
                    queues.Add(new AccountQueue(account.DisplayName, items));

                    return Option.None<ErrorResponse>();
                },
                error => Option.Some(error));

    private Unit Show(IReadOnlyList<AccountQueue> queues)
    {
        PruneRemembered(queues);
        accounts = [.. queues.Select(queue => new AccountItems(queue.AccountName, [.. queue.Items.OfType<SyncAttentionItem.Conflict>().Select(CreateItem)]))];
        SetLoadState(SyncOverviewLoadStateFactory.CreateLoaded());
        ApplyFilter();

        return Unit.Instance;
    }

    private void PruneRemembered(IReadOnlyList<AccountQueue> queues)
    {
        var open = queues.SelectMany(queue => queue.Items).OfType<SyncAttentionItem.Conflict>().Select(conflict => conflict.Id).ToHashSet();

        foreach (var id in errorKeys.Keys.Where(id => !open.Contains(id)))
            _ = errorKeys.TryRemove(id, out _);

        foreach (var id in skipped.Keys.Where(id => !open.Contains(id)))
            _ = skipped.TryRemove(id, out _);
    }

    private Unit NotRead(ErrorResponse error)
    {
        ConflictListLog.ReadFailed(logger);
        Fail();

        return Unit.Instance;
    }

    private Unit ReadThrew(Exception exception)
    {
        ConflictListLog.ReadThrew(logger, exception.ErrorType());
        Fail();

        return Unit.Instance;
    }

    private void Fail()
    {
        accounts = [];
        SetLoadState(SyncOverviewLoadStateFactory.CreateFailed());
        ApplyFilter();
    }

    private void SetLoadState(SyncOverviewLoadState state)
    {
        LoadState = state;
        OnPropertyChanged(nameof(LoadState));
        OnPropertyChanged(nameof(LoadErrorText));
    }

    private void ApplyFilter()
    {
        Groups = [.. accounts.Select(account => (account.AccountName, Shown: (IReadOnlyList<ConflictItemViewModel>)[.. account.Items.Where(Passes)])).Where(account => account.Shown.Count > 0).Select(account => new ConflictAccountGroupViewModel(account.AccountName, account.Shown, Localization))];
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private bool Passes(ConflictItemViewModel item) => selectedFilter.Kind.Match(kind => item.Kind == kind, () => true);

    private ConflictItemViewModel CreateItem(SyncAttentionItem.Conflict conflict) =>
        new(conflict, planner, ResolveAsync, errorKeys.TryGetValue(conflict.Id, out var key) ? Option.Some(key) : Option.None<string>(), skipped.ContainsKey(conflict.Id), false, Localization);

    private async Task ResolveAsync(ConflictItemViewModel item, ConflictStrategy strategy, CancellationToken cancellationToken) =>
        await await RunAsync(() => resolver.ResolveAsync(item.AccountId, [item.Id], strategy, cancellationToken), cancellationToken)
            .MatchAsync(result => result.Match(summary => SettleAsync(item, strategy, summary, cancellationToken), error => RefuseAsync(item)), exception => ThrewAsync(item, exception));

    private Task SettleAsync(ConflictItemViewModel item, ConflictStrategy strategy, ResolutionSummary summary, CancellationToken cancellationToken)
    {
        _ = errorKeys.TryRemove(item.Id, out _);

        foreach (var failed in summary.Failed)
            errorKeys[failed.Id] = ConflictTexts.FailureKey(failed.Reason);

        if (strategy is ConflictStrategy.Skip && summary.Failed.All(failed => failed.Id != item.Id))
            _ = skipped.TryAdd(item.Id, 0);

        return LoadAsync(cancellationToken);
    }

    private Task RefuseAsync(ConflictItemViewModel item)
    {
        ConflictListLog.ResolveRefused(logger);
        Remember(item, ConflictTexts.RefusedKey);

        return Task.CompletedTask;
    }

    private Task ThrewAsync(ConflictItemViewModel item, Exception exception)
    {
        ConflictListLog.ResolveThrew(logger, exception.ErrorType());
        Remember(item, ConflictTexts.UnexpectedKey);

        return Task.CompletedTask;
    }

    private void Remember(ConflictItemViewModel item, string key)
    {
        errorKeys[item.Id] = key;
        item.ShowError(key);
    }

    private readonly record struct AccountQueue(string AccountName, IReadOnlyList<SyncAttentionItem> Items);

    private readonly record struct AccountItems(string AccountName, IReadOnlyList<ConflictItemViewModel> Items);
}
