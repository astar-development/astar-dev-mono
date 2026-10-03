using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TagDetail;

/// <summary>Links a <see cref="FileEntity"/> to a <see cref="TagEntity"/>. Pure join row, keyed by the pair.</summary>
public sealed class FileTagEntity
{
    /// <summary>Foreign key to the linked file.</summary>
    public required FileId FileId { get; set; }

    /// <summary>Navigation property to the linked file.</summary>
    public FileEntity FileDetail { get; set; } = null!;

    /// <summary>Foreign key to the linked tag.</summary>
    public required TagId TagId { get; set; }

    /// <summary>Navigation property to the linked tag.</summary>
    public TagEntity Tag { get; set; } = null!;
}
