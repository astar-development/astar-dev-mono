using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Search;

public sealed partial class SyncedFileResultViewModel : ObservableObject
{
    private readonly IFileOpenerService fileOpenerService;
    private readonly IUiDispatcher dispatcher;

    public SyncedFileResultViewModel(SyncedItemSearchResult result, IFileTypeClassifier fileTypeClassifier, IFileOpenerService fileOpenerService, IUiDispatcher dispatcher)
    {
        this.fileOpenerService = fileOpenerService;
        this.dispatcher = dispatcher;
        FileName = Path.GetFileName(result.LocalPath);
        FormattedSize = FormatSize(result.SizeInBytes);
        TagName = string.Join(", ", result.TagNames);
        LocalPath = result.LocalPath;
        FileType = fileTypeClassifier.Classify(Path.GetExtension(result.LocalPath));
        IsLocalPresent = File.Exists(result.LocalPath);
    }

    public string FileName { get; }
    public string FormattedSize { get; }
    public string TagName { get; }
    public string LocalPath { get; }
    public FileType FileType { get; }
    public bool IsLocalPresent { get; }

    [ObservableProperty]
    public partial IImage? Thumbnail { get; set; }

    [RelayCommand(CanExecute = nameof(IsLocalPresent))]
    private void OpenFile() => fileOpenerService.OpenFile(LocalPath);

    public async Task LoadThumbnailAsync()
    {
        if (!IsLocalPresent || FileType != FileType.Image)
            return;

        var bitmap = await Task.Run(() =>
        {
            using var stream = File.OpenRead(LocalPath);
            return Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, 150);
        });

        dispatcher.Post(() => Thumbnail = bitmap);
    }

    private static string FormatSize(long? bytes) => bytes switch
    {
        null => string.Empty,
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
