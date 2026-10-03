namespace AStarDev.ControlDb.FileDetail;

/// <summary>The dimensions of an image file.</summary>
public sealed class ImageDetailEntity
{
    /// <summary>Primary key.</summary>
    public required ImageId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>Foreign key to the parent <see cref="FileEntity"/>.</summary>
    public required FileId FileId { get; set; }

    /// <summary>Navigation property to the parent file detail.</summary>
    public FileEntity FileDetail { get; set; } = null!;

    /// <summary>The width of the image in pixels, or null if not an image.</summary>
    public int? Width { get; set; }

    /// <summary>The height of the image in pixels, or null if not an image.</summary>
    public int? Height { get; set; }
}
