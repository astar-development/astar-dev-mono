namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>A file discovered and tracked by the scraper.</summary>
public sealed class FileDetailEntity
{
    /// <summary>Primary key.</summary>
    public FileId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The file's name, excluding its directory path.</summary>
    public required FileName FileName { get; set; }

    /// <summary>The directory path containing the file, excluding the file name.</summary>
    public required DirectoryName DirectoryName { get; set; }

    /// <summary>An opaque, stable handle identifying the file across renames and moves.</summary>
    public FileHandle FileHandle { get; set; }

    /// <summary>The file size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Whether the file is of a supported image type.</summary>
    public bool IsImage { get; set; }

    /// <summary>The width of the file in pixels, if it is an image.</summary>
    public int Width { get; set; }

    /// <summary>The height of the file in pixels, if it is an image.</summary>
    public int Height { get; set; }

    /// <summary>Foreign key to the owned <see cref="FileAccessDetailEntity"/>.</summary>
    public int FileAccessDetailId { get; set; }

    /// <summary>Navigation property to the owned file access detail.</summary>
    public FileAccessDetailEntity FileAccessDetail { get; set; } = new();

    /// <summary>Foreign key to the owned <see cref="ImageDetailEntity"/>.</summary>
    public ImageId ImageDetailId { get; set; }

    /// <summary>Navigation property to the owned image detail.</summary>
    public ImageDetailEntity ImageDetail { get; set; } = new();

    /// <summary>Foreign key to the owned <see cref="DeletionStatusEntity"/>.</summary>
    public int DeletionStatusId { get; set; }

    /// <summary>Navigation property to the owned deletion status.</summary>
    public DeletionStatusEntity DeletionStatus { get; set; } = new();
}
