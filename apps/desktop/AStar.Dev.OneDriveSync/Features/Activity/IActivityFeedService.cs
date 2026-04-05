using System;
using System.Collections.Generic;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Features.Activity;

/// <summary>
///     In-memory, ephemeral feed of the last 50 sync activity items (S013).
///     Singleton; cleared on app restart. Implement alongside <see cref="AStar.Dev.Sync.Engine.Features.Activity.IActivityReporter"/>
///     to receive events from the sync engine.
/// </summary>
public interface IActivityFeedService
{
    /// <summary>Hot observable that emits each new <see cref="ActivityItem"/> as it arrives.</summary>
    IObservable<ActivityItem> ActivityStream { get; }

    /// <summary>
    ///     Returns a snapshot of the current feed (newest first, max 50 items).
    ///     Returns <see cref="Option{T}.None"/> when the feed is empty.
    /// </summary>
    Option<IReadOnlyList<ActivityItem>> GetSnapshot();
}
