namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The dimensions of an image file.</summary>
public sealed class ImageDetailEntity
{
    /// <summary>Primary key.</summary>
    public ImageId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The width of the image in pixels, or null if not an image.</summary>
    public int? Width { get; set; }

    /// <summary>The height of the image in pixels, or null if not an image.</summary>
    public int? Height { get; set; }
}
