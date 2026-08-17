namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The dimensions of an image file.</summary>
public sealed class ImageDetailEntity
{
    /// <summary>Primary key.</summary>
    public ImageId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>Foreign key to the parent <see cref="FileDetailEntity"/>.</summary>
    public FileId FileDetailId { get; set; }

    /// <summary>Navigation property to the parent file detail.</summary>
    public FileDetailEntity FileDetail { get; set; } = null!;

    /// <summary>The width of the image in pixels, or null if not an image.</summary>
    public int? Width { get; set; }

    /// <summary>The height of the image in pixels, or null if not an image.</summary>
    public int? Height { get; set; }
}
