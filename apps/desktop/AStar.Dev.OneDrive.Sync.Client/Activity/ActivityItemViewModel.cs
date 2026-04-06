using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public enum ActivityItemType { Downloaded, Uploaded, Deleted, Conflict, Error, Info }

public sealed partial class ActivityItemViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AccountId { get; init; } = string.Empty;
    public string AccountEmail { get; init; } = string.Empty;
    public string FolderName { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public ActivityItemType Type { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ErrorMessage { get; init; }
    public long FileSize { get; init; }

    public string TypeLabel => Type switch
    {
        ActivityItemType.Downloaded => "downloaded",
        ActivityItemType.Uploaded => "uploaded",
        ActivityItemType.Deleted => "deleted",
        ActivityItemType.Conflict => "conflict",
        ActivityItemType.Error => "error",
        _ => "info"
    };

    public string TypeIcon => Type switch
    {
        ActivityItemType.Downloaded => "\u2193",
        ActivityItemType.Uploaded => "\u2191",
        ActivityItemType.Deleted => "\u00D7",
        ActivityItemType.Conflict => "\u26A0",
        ActivityItemType.Error => "\u26A0",
        _ => "\u2022"
    };

    public string TimeAgoText
    {
        get
        {
            var elapsed = DateTimeOffset.UtcNow - OccurredAt;
            return elapsed switch
            {
                { TotalSeconds: < 60 } => "just now",
                { TotalMinutes: < 60 } td => $"{(int)td.TotalMinutes}m ago",
                { TotalHours: < 24 } td => $"{(int)td.TotalHours}h ago",
                { TotalDays: < 2 } => "yesterday",
                var td => $"{(int)td.TotalDays}d ago"
            };
        }
    }

    public string FileSizeText => FileSize.FileSizeToText();

    public static ActivityItemViewModel FromJob(SyncJob job, string accountEmail, string folderName = "") => new()
    {
        AccountId = job.AccountId,
        AccountEmail = accountEmail,
        FolderName = folderName,
        FileName = Path.GetFileName(job.RelativePath),
        RelativePath = job.RelativePath,
        Type = job.Direction switch
        {
            SyncDirection.Download => ActivityItemType.Downloaded,
            SyncDirection.Upload => ActivityItemType.Uploaded,
            SyncDirection.Delete => ActivityItemType.Deleted,
            _ => ActivityItemType.Info
        },
        FileSize = job.FileSize,
        OccurredAt = job.CompletedAt ?? DateTimeOffset.UtcNow,
        ErrorMessage = job.ErrorMessage
    };

    public static ActivityItemViewModel FromConflict(SyncConflict conflict, string accountEmail, string folderName) => new()
    {
        AccountId = conflict.AccountId,
        AccountEmail = accountEmail,
        FolderName = folderName,
        FileName = Path.GetFileName(conflict.RelativePath),
        RelativePath = conflict.RelativePath,
        Type = ActivityItemType.Conflict,
        OccurredAt = conflict.DetectedAt
    };

    public static ActivityItemViewModel Error(string accountId, string accountEmail, string message) => new()
    {
        AccountId = accountId,
        AccountEmail = accountEmail,
        Type = ActivityItemType.Error,
        FileName = "Sync error",
        ErrorMessage = message
    };
}
