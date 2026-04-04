using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Conflict.Resolution.Features.Resolution;
using AStar.Dev.OneDriveSync.Infrastructure;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Conflicts;

/// <summary>
///     View model for the cross-account conflict queue view (CR-06, CR-07, Section 7).
///     Subscribes to <see cref="IConflictStore.ConflictQueueChanged"/> on the main thread scheduler.
/// </summary>
public sealed class ConflictsViewModel : ViewModelBase, IDisposable
{
    private readonly IConflictStore _conflictStore;
    private readonly IConflictResolver _conflictResolver;
    private readonly ICascadeService _cascadeService;
    private readonly IDisposable _queueSubscription;

    public ConflictsViewModel(IConflictStore conflictStore, IConflictResolver conflictResolver, ICascadeService cascadeService)
    {
        _conflictStore   = conflictStore;
        _conflictResolver = conflictResolver;
        _cascadeService  = cascadeService;

        SelectAllCommand       = ReactiveCommand.Create(SelectAll);
        LocalWinsCommand       = ReactiveCommand.CreateFromTask(() => ApplyStrategyAsync(ResolutionStrategy.LocalWins));
        RemoteWinsCommand      = ReactiveCommand.CreateFromTask(() => ApplyStrategyAsync(ResolutionStrategy.RemoteWins));
        KeepBothCommand        = ReactiveCommand.CreateFromTask(() => ApplyStrategyAsync(ResolutionStrategy.KeepBoth));
        SkipCommand            = ReactiveCommand.CreateFromTask(() => ApplyStrategyAsync(ResolutionStrategy.Skip));

        _queueSubscription = _conflictStore.ConflictQueueChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnQueueChanged);
    }

    /// <summary>All pending conflict rows — newest first.</summary>
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    /// <summary>Live badge count for the nav rail icon (CR-06).</summary>
    public int BadgeCount
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Error from the most recent failed operation; null when no error.</summary>
    public string? ErrorMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
    public ReactiveCommand<Unit, Unit> LocalWinsCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoteWinsCommand { get; }
    public ReactiveCommand<Unit, Unit> KeepBothCommand { get; }
    public ReactiveCommand<Unit, Unit> SkipCommand { get; }

    /// <summary>Loads pending conflicts from the store. Call after construction.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var result = await _conflictStore.GetPendingAsync(ct).ConfigureAwait(false);

        if (result is not AStar.Dev.Functional.Extensions.Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok ok)
            return;

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            Conflicts.Clear();

            foreach (var record in ok.Value)
                Conflicts.Add(new ConflictItemViewModel(record));

            BadgeCount = Conflicts.Count;
        });
    }

    private void SelectAll()
    {
        foreach (var item in Conflicts)
            item.IsSelected = true;
    }

    private async Task ApplyStrategyAsync(ResolutionStrategy strategy)
    {
        var selected = Conflicts.Where(item => item.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        var pendingResult = await _conflictStore.GetPendingAsync().ConfigureAwait(false);

        if (pendingResult is not AStar.Dev.Functional.Extensions.Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok pending)
            return;

        foreach (var item in selected)
        {
            var record = pending.Value.FirstOrDefault(conflict => conflict.Id == item.ConflictId);

            if (record is null)
                continue;

            var resolveResult = await _conflictResolver.ResolveAsync(record, strategy).ConfigureAwait(false);

            if (resolveResult is AStar.Dev.Functional.Extensions.Result<ConflictRecord, ConflictResolverError>.Ok)
                await _cascadeService.ApplyCascadeAsync(record.Id, record.FilePath, strategy).ConfigureAwait(false);
        }

        await LoadAsync().ConfigureAwait(false);
    }

    private void OnQueueChanged(ConflictQueueChanged change)
    {
        BadgeCount = change.PendingCount;

        if (change.ChangeType is ConflictQueueChangeType.ConflictAdded or ConflictQueueChangeType.ConflictResolved)
            _ = Task.Run(() => LoadAsync());
    }

    /// <inheritdoc />
    public void Dispose() => _queueSubscription.Dispose();
}
