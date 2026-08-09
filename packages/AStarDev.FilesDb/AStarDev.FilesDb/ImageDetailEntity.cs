namespace AStarDev.FilesDb;

/// <summary>The dimensions of an image file.</summary>
public sealed class ImageDetailEntity
{
    /// <summary>Foreign key to the parent <see cref="FileEntity"/>.</summary>
    public FileId FileEntityId { get; set; }

    /// <summary>Navigation property to the parent file entity.</summary>
    public FileEntity FileDetail { get; set; } = null!;

    /// <summary>The width of the image in pixels, or null if not an image.</summary>
    public int? Width { get; set; }

    /// <summary>The height of the image in pixels, or null if not an image.</summary>
    public int? Height { get; set; }
}
