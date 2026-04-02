using System.Globalization;
using AStar.Dev.File.App.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.IO;

namespace AStar.Dev.File.App.ViewModels;

public class ScannedFileDisplayItem : ReactiveObject
{
    public int Id { get; }
    public string FullPath { get; }
    public string FileName { get; }
    public string FolderPath { get; }
    public string Extension { get; }
    public bool IsImage { get; }
    public long SizeInBytes { get; }
    public string FormattedSize { get; }
    public string FileType { get; }
    public string LastModified { get; }
    public string LastViewed { get; }

    [Reactive] public bool PendingDelete { get; set; }

    public ScannedFileDisplayItem(ScannedFile file)
    {
        Id = file.Id;
        FullPath = file.FullPath;
        FileName = file.FileName;
        FolderPath = file.FolderPath;
        Extension = Path.GetExtension(file.FileName).TrimStart('.').ToUpperInvariant();
        IsImage = file.FileType == Models.FileType.Image;
        SizeInBytes = file.SizeInBytes;
        FormattedSize = FormatSize(file.SizeInBytes);
        FileType = file.FileType.ToString();
        LastModified = file.LastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        LastViewed = file.LastViewed.HasValue
            ? file.LastViewed.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
            : "—";
        PendingDelete = file.PendingDelete;
    }

    public static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824L)
            return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576L)
            return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024L)
            return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }
}
