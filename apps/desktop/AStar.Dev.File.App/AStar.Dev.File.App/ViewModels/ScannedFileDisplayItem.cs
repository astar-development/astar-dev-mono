using System.Globalization;
using AStar.Dev.File.App.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AStar.Dev.File.App.ViewModels;

public class ScannedFileDisplayItem(ScannedFile file) : ReactiveObject
{
    public int Id { get; } = file.Id;
    public string FullPath { get; } = file.FullPath;
    public string FileName { get; } = file.FileName;
    public string FolderPath { get; } = file.FolderPath;
    public string Extension { get; } = Path.GetExtension(file.FileName).TrimStart('.').ToUpperInvariant();
    public bool IsImage { get; } = file.FileType == Models.FileType.Image;
    public long SizeInBytes { get; } = file.SizeInBytes;
    public string FormattedSize { get; } = FormatSize(file.SizeInBytes);
    public string FileType { get; } = file.FileType.ToString();
    public string LastModified { get; } = file.LastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
    public string LastViewed { get; } = file.LastViewed.HasValue
        ? file.LastViewed.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
        : "—";

    [Reactive] public bool PendingDelete { get; set; } = file.PendingDelete;

    public static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824L => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576L => $"{bytes / 1_048_576.0:F1} MB",
            >= 1_024L => $"{bytes / 1_024.0:F1} KB",
            _ => $"{bytes} B",
        };
    }
}
