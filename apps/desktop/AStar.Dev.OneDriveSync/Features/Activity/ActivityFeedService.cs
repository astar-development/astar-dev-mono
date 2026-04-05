using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.Sync.Engine.Features.Activity;

namespace AStar.Dev.OneDriveSync.Features.Activity;

/// <summary>
///     In-memory activity feed that bridges the sync engine (<see cref="IActivityReporter"/>)
///     to the Activity view (<see cref="IActivityFeedService"/>).
///     Singleton; ephemeral — not persisted to SQLite (S013, NF-16).
/// </summary>
public sealed class ActivityFeedService : IActivityFeedService, IActivityReporter, IDisposable
{
    private const int MaxItems = 50;

    private readonly Subject<ActivityItem> _subject = new();
    private readonly LinkedList<ActivityItem> _items = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public IObservable<ActivityItem> ActivityStream => _subject;

    /// <inheritdoc />
    public Option<IReadOnlyList<ActivityItem>> GetSnapshot()
    {
        lock (_lock)
        {
            if (_items.Count == 0)

                return Option<IReadOnlyList<ActivityItem>>.None.Instance;

            IReadOnlyList<ActivityItem> snapshot = [.. _items];

            return new Option<IReadOnlyList<ActivityItem>>.Some(snapshot);
        }
    }

    /// <inheritdoc />
    public void Report(string accountId, ActivityActionType actionType, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var item = ActivityItemFactory.Create(accountId, DateTimeOffset.UtcNow, actionType, filePath);

        lock (_lock)
        {
            _items.AddFirst(item);

            while (_items.Count > MaxItems)
                _items.RemoveLast();

            _subject.OnNext(item);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }
}
