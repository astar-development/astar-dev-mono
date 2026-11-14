using System.Collections.Concurrent;
using System.ComponentModel;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.ViewModels.Common;
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
    private readonly ConcurrentDictionary<ConflictId, byte> selected = new();
    private readonly ConcurrentDictionary<HeldDeletionId, string> heldErrorKeys = new();
    private readonly ConcurrentDictionary<HeldDeletionId, byte> deferred = new();
    private readonly HeldDeletionActions heldActions;
    private int loadsStarted;
    private int bulkBusy;
    private Option<string> bulkErrorKey = Option.None<string>();
    private IReadOnlyList<HeldDeletionItemViewModel> heldShown = [];
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
        heldActions = new HeldDeletionActions(ConfirmHeldAsync, RestoreHeldAsync, DecideLater);
        BulkOptions =
        [
            CreateBulkOption(ConflictStrategyFactory.CreateLocalWins()),
            CreateBulkOption(ConflictStrategyFactory.CreateRemoteWins()),
            CreateBulkOption(ConflictStrategyFactory.CreateKeepBoth()),
            CreateBulkOption(ConflictStrategyFactory.CreateSkip())
        ];
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
            OnPropertyChanged(nameof(DefaultStrategy));
            OnPropertyChanged(nameof(HasDefault));
            OnPropertyChanged(nameof(DefaultText));
            OnPropertyChanged(nameof(DefaultAutomationName));
            NotifyBulk();
        }
    }

    /// <summary>Whether any conflict passes the filter.</summary>
    public bool HasConflicts => Groups.Count > 0;

    /// <summary>Whether any held deletion set is shown.</summary>
    public bool HasHeld => heldShown.Count > 0;

    /// <summary>Whether the read succeeded and nothing is listed.</summary>
    public bool IsEmpty => LoadState is SyncOverviewLoadState.Loaded && Groups.Count == 0 && heldShown.Count == 0;

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
    public IReadOnlyList<HeldDeletionItemViewModel> HeldDeletions => heldShown;

    /// <summary>How many listed conflicts are ticked.</summary>
    public int SelectedCount => selected.Count;

    /// <summary>The ticked count in words.</summary>
    public string SelectedText => Localization.GetLocal("Conflict.Selected", SelectedCount.ToString("N0", Localization.CurrentCulture));

    /// <summary>The text of the Select all button.</summary>
    public string SelectAllText => Localization.GetLocal("Conflict.SelectAll");

    /// <summary>The text of the Clear selection button.</summary>
    public string ClearSelectionText => Localization.GetLocal("Conflict.ClearSelection");

    /// <summary>The label of the bulk strategy buttons.</summary>
    public string BulkLabelText => Localization.GetLocal("Conflict.Bulk.Label");

    /// <summary>The heading of the held deletions section.</summary>
    public string HeldTitleText => Localization.GetLocal("Held.Title");

    /// <summary>The error of the last bulk apply; empty when there is none.</summary>
    public string BulkErrorText => bulkErrorKey.Match(Localization.GetLocal, () => string.Empty);

    /// <summary>Ticks every listed conflict.</summary>
    public IRelayCommand SelectAllCommand => field ??= new RelayCommand(SelectAll);

    /// <summary>Unticks every conflict.</summary>
    public IRelayCommand ClearSelectionCommand => field ??= new RelayCommand(ClearSelection);

    /// <summary>One button per strategy; each applies its strategy to the ticked conflicts.</summary>
    public IReadOnlyList<ConflictBulkOptionViewModel> BulkOptions { get; }

    /// <summary>The default strategy of the shown kind, or none when every kind is shown.</summary>
    public Option<ConflictStrategy> DefaultStrategy => selectedFilter.Kind.Match(kind => Option.Some(DefaultFor(kind)), () => Option.None<ConflictStrategy>());

    /// <summary>Whether a kind is shown, so a default strategy is offered.</summary>
    public bool HasDefault => DefaultStrategy.Match(_ => true, () => false);

    /// <summary>The text of the button that applies the default strategy.</summary>
    public string DefaultText => DefaultStrategy.Match(strategy => Localization.GetLocal("Conflict.Default.Text", Localization.GetLocal(ConflictTexts.StrategyKey(strategy))), () => string.Empty);

    /// <summary>The accessible name of the default strategy button.</summary>
    public string DefaultAutomationName => DefaultText;

    /// <summary>Applies the default strategy of the shown kind to the ticked conflicts.</summary>
    public IAsyncRelayCommand DefaultCommand => field ??= new AsyncRelayCommand(ApplyDefaultAsync, () => HasDefault && CanApply());

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
                async items =>
                {
                    var pairNames = await ReadPairNamesAsync(account.Id, items, cancellationToken);
                    queues.Add(new AccountQueue(account.DisplayName, items, pairNames));

                    return Option.None<ErrorResponse>();
                },
                error => Task.FromResult(Option.Some(error)));

    private async Task<IReadOnlyDictionary<SyncPairId, string>> ReadPairNamesAsync(AccountId accountId, IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken)
    {
        if (!items.OfType<SyncAttentionItem.HeldDeletions>().Any())
            return new Dictionary<SyncPairId, string>();

        return await sources.Pairs.GetAsync(accountId, cancellationToken)
            .MatchAsync(pairs => (IReadOnlyDictionary<SyncPairId, string>)pairs.ToDictionary(pair => pair.Id, pair => pair.Name), _ => new Dictionary<SyncPairId, string>());
    }

    private Unit Show(IReadOnlyList<AccountQueue> queues)
    {
        PruneRemembered(queues);
        accounts = [.. queues.Select(queue => new AccountItems(queue.AccountName, [.. queue.Items.OfType<SyncAttentionItem.Conflict>().Select(CreateItem)], [.. queue.Items.OfType<SyncAttentionItem.HeldDeletions>().Select(held => CreateHeldItem(queue, held))]))];
        SetLoadState(SyncOverviewLoadStateFactory.CreateLoaded());
        ApplyFilter();

        return Unit.Instance;
    }

    private void PruneRemembered(IReadOnlyList<AccountQueue> queues)
    {
        var openItems = queues.SelectMany(queue => queue.Items).ToList();
        var open = openItems.OfType<SyncAttentionItem.Conflict>().Select(conflict => conflict.Id).ToHashSet();
        var openHeld = openItems.OfType<SyncAttentionItem.HeldDeletions>().Select(held => held.Id).ToHashSet();

        foreach (var id in errorKeys.Keys.Where(id => !open.Contains(id)))
            _ = errorKeys.TryRemove(id, out _);

        foreach (var id in skipped.Keys.Where(id => !open.Contains(id)))
            _ = skipped.TryRemove(id, out _);

        foreach (var id in selected.Keys.Where(id => !open.Contains(id)))
            _ = selected.TryRemove(id, out _);

        foreach (var id in heldErrorKeys.Keys.Where(id => !openHeld.Contains(id)))
            _ = heldErrorKeys.TryRemove(id, out _);

        foreach (var id in deferred.Keys.Where(id => !openHeld.Contains(id)))
            _ = deferred.TryRemove(id, out _);
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
        selected.Clear();
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
        foreach (var hidden in accounts.SelectMany(account => account.Items).Where(item => item.IsSelected && !Passes(item)))
            hidden.IsSelected = false;

        Groups = [.. accounts.Select(account => (account.AccountName, Shown: (IReadOnlyList<ConflictItemViewModel>)[.. account.Items.Where(Passes)])).Where(account => account.Shown.Count > 0).Select(account => new ConflictAccountGroupViewModel(account.AccountName, account.Shown, Localization))];
        heldShown = selectedFilter.Kind.Match(_ => [], () => (IReadOnlyList<HeldDeletionItemViewModel>)[.. accounts.SelectMany(account => account.Held)]);
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(HeldDeletions));
        OnPropertyChanged(nameof(HasConflicts));
        OnPropertyChanged(nameof(HasHeld));
        OnPropertyChanged(nameof(IsEmpty));
        SelectionChanged();
    }

    private bool Passes(ConflictItemViewModel item) => selectedFilter.Kind.Match(kind => item.Kind == kind, () => true);

    private ConflictItemViewModel CreateItem(SyncAttentionItem.Conflict conflict)
    {
        var item = new ConflictItemViewModel(conflict, planner, ResolveAsync, errorKeys.TryGetValue(conflict.Id, out var key) ? Option.Some(key) : Option.None<string>(), skipped.ContainsKey(conflict.Id), selected.ContainsKey(conflict.Id), Localization);
        item.PropertyChanged += OnItemChanged;

        return item;
    }

    private HeldDeletionItemViewModel CreateHeldItem(AccountQueue queue, SyncAttentionItem.HeldDeletions held)
    {
        var pairName = queue.PairNames.TryGetValue(held.PairId, out var name) ? name : Localization.GetLocal("Held.PairUnknown");

        return new HeldDeletionItemViewModel(held, queue.AccountName, pairName, heldActions, heldErrorKeys.TryGetValue(held.Id, out var key) ? Option.Some(key) : Option.None<string>(), deferred.ContainsKey(held.Id), Localization);
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ConflictItemViewModel.IsSelected) || sender is not ConflictItemViewModel item)
            return;

        if (item.IsSelected)
            _ = selected.TryAdd(item.Id, 0);
        else
            _ = selected.TryRemove(item.Id, out _);

        SelectionChanged();
    }

    private void SelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedText));
        NotifyBulk();
    }

    private void SelectAll()
    {
        foreach (var item in Groups.SelectMany(group => group.Items))
            item.IsSelected = true;
    }

    private void ClearSelection()
    {
        foreach (var item in accounts.SelectMany(account => account.Items))
            item.IsSelected = false;
    }

    private bool CanApply() => !selected.IsEmpty && Volatile.Read(ref bulkBusy) == 0;

    private void NotifyBulk()
    {
        foreach (var option in BulkOptions)
            option.Command.NotifyCanExecuteChanged();

        DefaultCommand.NotifyCanExecuteChanged();
    }

    private static ConflictStrategy DefaultFor(ConflictKind kind) => kind is ConflictKind.MoveTargetTaken ? ConflictStrategyFactory.CreateSkip() : ConflictStrategyFactory.CreateKeepBoth();

    private ConflictBulkOptionViewModel CreateBulkOption(ConflictStrategy strategy) =>
        new(strategy, new AsyncRelayCommand(cancellationToken => ApplyAsync(strategy, cancellationToken), CanApply), Localization);

    private Task ApplyDefaultAsync(CancellationToken cancellationToken) => DefaultStrategy.Match(strategy => ApplyAsync(strategy, cancellationToken), () => Task.CompletedTask);

    private async Task ApplyAsync(ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref bulkBusy, 1, 0) != 0)
            return;

        using var scope = new BulkScope(this);
        NotifyBulk();
        SetBulkError(Option.None<string>());

        foreach (var account in Groups.SelectMany(group => group.Items).Where(item => item.IsSelected && !item.IsBusy).GroupBy(item => item.AccountId).ToList())
            await ApplyToAccountAsync(account.Key, [.. account], strategy, cancellationToken);

        ClearSelection();
        await LoadAsync(cancellationToken);
    }

    private async Task ApplyToAccountAsync(AccountId accountId, IReadOnlyList<ConflictItemViewModel> items, ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        IReadOnlyList<ConflictId> ids = [.. items.Select(item => item.Id)];

        await await RunAsync(() => resolver.ResolveAsync(accountId, ids, strategy, cancellationToken), cancellationToken)
            .MatchAsync(result => result.Match(summary => Recorded(ids, strategy, summary), error => RefuseAllAsync(items)), exception => ThrewAllAsync(items, exception));
    }

    private Task Recorded(IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, ResolutionSummary summary)
    {
        Record(ids, strategy, summary);

        return Task.CompletedTask;
    }

    private Task RefuseAllAsync(IReadOnlyList<ConflictItemViewModel> items)
    {
        ConflictListLog.ResolveRefused(logger);
        RememberAll(items, ConflictTexts.RefusedKey);

        return Task.CompletedTask;
    }

    private Task ThrewAllAsync(IReadOnlyList<ConflictItemViewModel> items, Exception exception)
    {
        ConflictListLog.ResolveThrew(logger, exception.ErrorType());
        RememberAll(items, ConflictTexts.UnexpectedKey);

        return Task.CompletedTask;
    }

    private void RememberAll(IReadOnlyList<ConflictItemViewModel> items, string key)
    {
        foreach (var item in items)
            Remember(item, key);

        SetBulkError(Option.Some(key));
    }

    private void SetBulkError(Option<string> key)
    {
        bulkErrorKey = key;
        OnPropertyChanged(nameof(BulkErrorText));
    }

    private Task ConfirmHeldAsync(HeldDeletionItemViewModel item, CancellationToken cancellationToken) => SettleHeldAsync(item, () => heldDecider.ConfirmAsync(item.AccountId, item.Id, cancellationToken), cancellationToken);

    private Task RestoreHeldAsync(HeldDeletionItemViewModel item, CancellationToken cancellationToken) => SettleHeldAsync(item, () => heldDecider.RestoreAsync(item.AccountId, item.Id, cancellationToken), cancellationToken);

    private async Task SettleHeldAsync(HeldDeletionItemViewModel item, Func<Task<Result<Unit, ErrorResponse>>> settle, CancellationToken cancellationToken) =>
        await await RunAsync(settle, cancellationToken)
            .MatchAsync(result => result.Match(_ => HeldSettledAsync(item, cancellationToken), _ => HeldNotSettledAsync(item)), exception => HeldThrewAsync(item, exception));

    private Task HeldSettledAsync(HeldDeletionItemViewModel item, CancellationToken cancellationToken)
    {
        _ = heldErrorKeys.TryRemove(item.Id, out _);
        _ = deferred.TryRemove(item.Id, out _);

        return LoadAsync(cancellationToken);
    }

    private Task HeldNotSettledAsync(HeldDeletionItemViewModel item)
    {
        ConflictListLog.HeldNotSettled(logger);
        RememberHeld(item, ConflictTexts.HeldNotSettledKey);

        return Task.CompletedTask;
    }

    private Task HeldThrewAsync(HeldDeletionItemViewModel item, Exception exception)
    {
        ConflictListLog.HeldThrew(logger, exception.ErrorType());
        RememberHeld(item, ConflictTexts.UnexpectedKey);

        return Task.CompletedTask;
    }

    private void RememberHeld(HeldDeletionItemViewModel item, string key)
    {
        heldErrorKeys[item.Id] = key;
        item.ShowError(key);
    }

    private void DecideLater(HeldDeletionItemViewModel item)
    {
        _ = deferred.TryAdd(item.Id, 0);
        item.MarkDeferred();
    }

    private async Task ResolveAsync(ConflictItemViewModel item, ConflictStrategy strategy, CancellationToken cancellationToken) =>
        await await RunAsync(() => resolver.ResolveAsync(item.AccountId, [item.Id], strategy, cancellationToken), cancellationToken)
            .MatchAsync(result => result.Match(summary => SettleAsync(item, strategy, summary, cancellationToken), error => RefuseAsync(item)), exception => ThrewAsync(item, exception));

    private Task SettleAsync(ConflictItemViewModel item, ConflictStrategy strategy, ResolutionSummary summary, CancellationToken cancellationToken)
    {
        Record([item.Id], strategy, summary);

        return LoadAsync(cancellationToken);
    }

    private void Record(IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, ResolutionSummary summary)
    {
        foreach (var id in ids)
            _ = errorKeys.TryRemove(id, out _);

        foreach (var failed in summary.Failed)
            errorKeys[failed.Id] = ConflictTexts.FailureKey(failed.Reason);

        if (strategy is not ConflictStrategy.Skip)
            return;

        foreach (var id in ids.Where(id => summary.Failed.All(failed => failed.Id != id)))
            _ = skipped.TryAdd(id, 0);
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

    private sealed class BulkScope(ConflictListViewModel owner) : IDisposable
    {
        public void Dispose()
        {
            Volatile.Write(ref owner.bulkBusy, 0);
            owner.NotifyBulk();
        }
    }

    private readonly record struct AccountQueue(string AccountName, IReadOnlyList<SyncAttentionItem> Items, IReadOnlyDictionary<SyncPairId, string> PairNames);

    private readonly record struct AccountItems(string AccountName, IReadOnlyList<ConflictItemViewModel> Items, IReadOnlyList<HeldDeletionItemViewModel> Held);
}
