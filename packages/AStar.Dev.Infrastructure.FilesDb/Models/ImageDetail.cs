namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// </summary>
public sealed class ImageDetail
{
    /// <summary>
    /// </summary>
    public ImageId Id { get; set; } = ImageId.CreateNew;

    /// <summary>
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// </summary>
    public int? Height { get; set; }
}
