using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class ActivityItemViewModel : ReactiveObject
{
    public string TypeIcon { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FolderName { get; init; } = string.Empty;
    public string FileSizeText { get; init; } = string.Empty;
    public string TimeAgoText { get; init; } = string.Empty;
}
