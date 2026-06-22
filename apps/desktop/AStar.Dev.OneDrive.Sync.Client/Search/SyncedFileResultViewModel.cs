using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Search;

public sealed partial class SyncedFileResultViewModel : ObservableObject
{
    private readonly IFileOpenerService fileOpenerService;
    private readonly IUiDispatcher dispatcher;
    private readonly ILocalizationService loc;
    private readonly Func<CancellationToken, Task> onDeleteAsync;
    private CancellationTokenSource? thumbnailCts;

    public SyncedFileResultViewModel(SyncedItemSearchResult result, IFileTypeClassifier fileTypeClassifier, IFileOpenerService fileOpenerService, IUiDispatcher dispatcher, ILocalizationService loc, Func<CancellationToken, Task> onDeleteAsync)
    {
        this.fileOpenerService = fileOpenerService;
        this.dispatcher = dispatcher;
        this.loc = loc;
        this.onDeleteAsync = onDeleteAsync;
        FileName = Path.GetFileName(result.LocalPath);
        FormattedSize = FormatSize(result.SizeInBytes);
        TagName = string.Join(", ", result.TagNames);
        LocalPath = result.LocalPath;
        FileType = fileTypeClassifier.Classify(Path.GetExtension(result.LocalPath));
        IsLocalPresent = File.Exists(result.LocalPath);
    }

    /// <summary>File name extracted from the local path.</summary>
    public string FileName { get; }

    /// <summary>Human-readable file size (e.g. "1.2 MB").</summary>
    public string FormattedSize { get; }

    /// <summary>Comma-separated list of tag names associated with this file.</summary>
    public string TagName { get; }

    /// <summary>Absolute local path of the synced file.</summary>
    public string LocalPath { get; }

    /// <summary>Classified type of the file (Image, Document, etc.).</summary>
    public FileType FileType { get; }

    /// <summary>True when the file exists at <see cref="LocalPath"/>.</summary>
    public bool IsLocalPresent { get; }

    /// <summary>Card opacity — reduced when the local file is absent.</summary>
    public double CardOpacity => IsLocalPresent ? 1.0 : 0.4;

    /// <summary>Localised label for the delete button.</summary>
    public string DeleteButtonText => loc.GetLocal("Search.Result.Delete.Button");

    [ObservableProperty]
    public partial IImage? Thumbnail { get; set; }

    [RelayCommand(CanExecute = nameof(IsLocalPresent))]
    private void OpenFile() => fileOpenerService.OpenFile(LocalPath);

    [RelayCommand]
    private Task DeleteFileAsync(CancellationToken ct) => onDeleteAsync(ct);

    /// <summary>
    /// Cancels any in-progress thumbnail load and clears any thumbnail already set by a racing load.
    /// Safe to call from any thread; idempotent.
    /// </summary>
    public void CancelThumbnailLoad()
    {
        var cts = Interlocked.Exchange(ref thumbnailCts, null);
        cts?.Cancel();
        dispatcher.Post(() => Thumbnail = null);
    }

    /// <summary>Loads a 150-px-wide thumbnail from <see cref="LocalPath"/> when the file is a locally-present image.</summary>
    public async Task LoadThumbnailAsync()
    {
        var previous = Interlocked.Exchange(ref thumbnailCts, null);
        previous?.Cancel();

        var cts = new CancellationTokenSource();
        thumbnailCts = cts;

        if (!IsLocalPresent || FileType != FileType.Image)
            return;

        try
        {
            var bitmap = await Task.Run(() =>
            {
                using var stream = File.OpenRead(LocalPath);
                return Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, 150);
            }, cts.Token);

            if (!cts.IsCancellationRequested)
                dispatcher.Post(() => Thumbnail = bitmap);
        }
        catch (OperationCanceledException) { }
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
