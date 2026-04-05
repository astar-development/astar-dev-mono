using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AStar.Dev.OneDriveSync.Infrastructure;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Activity;

/// <summary>
///     View model for the Activity feed (S013).
///     Subscribes to <see cref="IActivityFeedService.ActivityStream"/> on the main-thread scheduler,
///     batching high-frequency updates per NF-02.
/// </summary>
public sealed class ActivityViewModel : ViewModelBase, IDisposable
{
    private const int MaxItems = 50;

    private readonly IDisposable _subscription;

    /// <summary>Initialises the view model, pre-populates from the existing snapshot, and subscribes to the live stream.</summary>
    public ActivityViewModel(IActivityFeedService activityFeedService) : this(activityFeedService, null) { }

    /// <summary>Test-only constructor — allows injecting a deterministic <paramref name="bufferScheduler"/>.</summary>
    internal ActivityViewModel(IActivityFeedService activityFeedService, IScheduler? bufferScheduler)
    {
        ArgumentNullException.ThrowIfNull(activityFeedService);

        _ = activityFeedService.GetSnapshot().Match(
            onSome: existingItems => { LoadSnapshot(existingItems); return 0; },
            onNone: () => 0);

        IsEmpty = Items.Count == 0;

        Items.CollectionChanged += (_, _) => IsEmpty = Items.Count == 0;

        var effectiveScheduler = bufferScheduler ?? RxApp.TaskpoolScheduler;

        _subscription = activityFeedService.ActivityStream
            .Buffer(TimeSpan.FromMilliseconds(200), effectiveScheduler)
            .Where(batch => batch.Count > 0)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ApplyBatch);
    }

    /// <summary>Activity items, newest first, capped at 50.</summary>
    public ObservableCollection<ActivityItemViewModel> Items { get; } = [];

    /// <summary><see langword="true"/> when no activity has been recorded yet.</summary>
    public bool IsEmpty
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();

    private void LoadSnapshot(IReadOnlyList<ActivityItem> items)
    {
        foreach (var item in items.OrderByDescending(static i => i.Timestamp))
            Items.Add(new ActivityItemViewModel(item));
    }

    private void ApplyBatch(IList<ActivityItem> batch)
    {
        foreach (var item in batch)
            Items.Insert(0, new ActivityItemViewModel(item));

        while (Items.Count > MaxItems)
            Items.RemoveAt(Items.Count - 1);
    }
}
