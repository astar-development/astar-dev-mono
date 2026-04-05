using AStar.Dev.Sync.Engine.Features.Activity;

namespace AStar.Dev.OneDriveSync.Features.Activity;

/// <summary>Immutable snapshot of a single sync file operation shown in the Activity feed (S013).</summary>
public sealed record ActivityItem(string AccountId, DateTimeOffset Timestamp, ActivityActionType ActionType, string FilePath);

/// <summary>Factory for creating validated <see cref="ActivityItem"/> instances.</summary>
public static class ActivityItemFactory
{
    /// <summary>Creates an <see cref="ActivityItem"/> with validated parameters.</summary>
    public static ActivityItem Create(string accountId, DateTimeOffset timestamp, ActivityActionType actionType, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return new ActivityItem(accountId, timestamp, actionType, filePath);
    }
}
