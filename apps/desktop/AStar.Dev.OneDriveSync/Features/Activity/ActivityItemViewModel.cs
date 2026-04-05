using System;
using System.IO;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.Sync.Engine.Features.Activity;

namespace AStar.Dev.OneDriveSync.Features.Activity;

/// <summary>Display wrapper for a single <see cref="ActivityItem"/> row in the Activity list (S013).</summary>
public sealed class ActivityItemViewModel : ViewModelBase
{
    /// <summary>Initialises a new view model from the given <paramref name="item"/>.</summary>
    public ActivityItemViewModel(ActivityItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        AccountId = item.AccountId;
        Timestamp = item.Timestamp;
        ActionType = item.ActionType;
        FilePath = item.FilePath;
        FileName = Path.GetFileName(item.FilePath);
    }

    /// <summary>Account identifier that performed the operation.</summary>
    public string AccountId { get; }

    /// <summary>UTC timestamp of the operation.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Type of file operation.</summary>
    public ActivityActionType ActionType { get; }

    /// <summary>Full file path.</summary>
    public string FilePath { get; }

    /// <summary>File name extracted from <see cref="FilePath"/> for compact display.</summary>
    public string FileName { get; }
}
