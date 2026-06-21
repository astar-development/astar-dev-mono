using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Activity;

public enum ActivityItemType { Downloaded, Uploaded, Deleted, Conflict, Error, Info }

public sealed partial class ActivityItemViewModel : ObservableObject
{
    private readonly ILocalizationService loc;

    public ActivityItemViewModel(ILocalizationService loc) => this.loc = loc;

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
        ActivityItemType.Downloaded => loc.GetLocal("Activity.Downloaded"),
        ActivityItemType.Uploaded   => loc.GetLocal("Activity.Uploaded"),
        ActivityItemType.Deleted    => loc.GetLocal("Activity.Deleted"),
        ActivityItemType.Conflict   => loc.GetLocal("Activity.Conflict"),
        ActivityItemType.Error      => loc.GetLocal("Activity.Error"),
        _                           => loc.GetLocal("Activity.Info")
    };

    public string TypeIcon => Type switch
    {
        ActivityItemType.Downloaded => "↓",
        ActivityItemType.Uploaded   => "↑",
        ActivityItemType.Deleted    => "×",
        ActivityItemType.Conflict   => "⚠",
        ActivityItemType.Error      => "⚠",
        _                           => "•"
    };

    public string TimeAgoText
    {
        get
        {
            var elapsed = DateTimeOffset.UtcNow - OccurredAt;

            return elapsed switch
            {
                { TotalSeconds: < 60 }  => loc.GetLocal("Common.JustNow"),
                { TotalMinutes: < 60 } td => $"{(int)td.TotalMinutes}m ago",
                { TotalHours: < 24 } td  => $"{(int)td.TotalHours}h ago",
                { TotalDays: < 2 }       => loc.GetLocal("Common.Yesterday"),
                var td                   => $"{(int)td.TotalDays}d ago"
            };
        }
    }

    public string FileSizeText => FileSize.FileSizeToText();

    /// <summary>Raises <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> for <see cref="TimeAgoText"/> so bound UI refreshes the relative timestamp.</summary>
    public void RefreshTimeAgoText() => OnPropertyChanged(nameof(TimeAgoText));

    public static ActivityItemViewModel FromJob(SyncJob job, ILocalizationService loc, string accountEmail, string folderName = "") => new(loc)
    {
        AccountId = job.Remote.AccountId.Id,
        AccountEmail = accountEmail,
        FolderName = folderName,
        FileName = Path.GetFileName(job.Target.RelativePath),
        RelativePath = job.Target.RelativePath,
        Type = job switch
        {
            DownloadSyncJob => ActivityItemType.Downloaded,
            UploadSyncJob   => ActivityItemType.Uploaded,
            DeleteSyncJob   => ActivityItemType.Deleted,
            _               => ActivityItemType.Info
        },
        FileSize = job.Metadata.FileSize,
        OccurredAt = job.Status.CompletedAt.MapOrDefault(v => v, DateTimeOffset.UtcNow),
        ErrorMessage = job.Status.ErrorMessage.Match<string?>(v => v, () => null)
    };

    public static ActivityItemViewModel FromConflict(SyncConflict conflict, ILocalizationService loc, string accountEmail, string folderName) => new(loc)
    {
        AccountId = conflict.Remote.AccountId.Id,
        AccountEmail = accountEmail,
        FolderName = folderName,
        FileName = Path.GetFileName(conflict.Target.RelativePath),
        RelativePath = conflict.Target.RelativePath,
        Type = ActivityItemType.Conflict,
        OccurredAt = conflict.DetectedAt
    };

    public static ActivityItemViewModel Error(string accountId, ILocalizationService loc, string accountEmail, string message) => new(loc)
    {
        AccountId = accountId,
        AccountEmail = accountEmail,
        Type = ActivityItemType.Error,
        FileName = loc.GetLocal("Activity.SyncError"),
        ErrorMessage = message
    };
}
