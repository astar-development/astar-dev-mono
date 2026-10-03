using AStarDev.ControlDb.TagDetail;

namespace AStarDev.ControlDb.FileDetail;

/// <summary>A file discovered and tracked by the scraper.</summary>
public sealed class FileEntity : IAggregateRoot
{
    /// <summary>Primary key.</summary>
    public FileId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The file's name, excluding its directory path.</summary>
    public required FileName FileName { get; set; }

    /// <summary>The directory path containing the file, excluding the file name.</summary>
    public required DirectoryName DirectoryName { get; set; }

    /// <summary>An opaque, stable handle identifying the file across renames and moves.</summary>
    public required FileHandle FileHandle { get; set; }

    /// <summary>The file size in bytes.</summary>
    public required long FileSize { get; set; }

    /// <summary>Whether the file is of a supported image type.</summary>
    public bool IsImage { get; set; }

    /// <summary>The date and time the file was last updated; for a scraped wallpaper, when it was downloaded and recorded.</summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Navigation property to the owned file access detail.</summary>
    public FileAccessDetailEntity FileAccessDetail { get; set; } = new() { Id = FileAccessDetailId.Create(), FileId = FileId.Create() };

    /// <summary>Navigation property to the owned image detail.</summary>
    public ImageDetailEntity? ImageDetail { get; set; }

    /// <summary>Navigation property to the owned deletion status.</summary>
    public DeletionStatusEntity? DeletionStatus { get; set; }

    /// <summary> The file's MIME type or extension, indicating its format.</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Navigation property to the tags linked to this file.</summary>
    public ICollection<FileTagEntity> FileTags { get; } = [];
}
