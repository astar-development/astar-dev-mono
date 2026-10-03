namespace AStarDev.ControlDb.TagDetail;

/// <summary>A Wallhaven tag, deduplicated by <see cref="WallhavenTagId"/> and shared across every file it's assigned to.</summary>
public sealed class TagEntity : IAggregateRoot
{
    /// <summary>Primary key.</summary>
    public TagId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>Wallhaven's own numeric id for this tag - the natural key used to avoid storing duplicates.</summary>
    public required int WallhavenTagId { get; set; }

    /// <summary>The tag's display name.</summary>
    public required string Name { get; set; }

    /// <summary>The tag's URL-safe alias.</summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>Wallhaven's category id for this tag.</summary>
    public int CategoryId { get; set; }

    /// <summary>The tag's category name.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The tag's purity rating.</summary>
    public string Purity { get; set; } = string.Empty;

    /// <summary>When true, any image carrying this tag is ignored (not downloaded or recorded).</summary>
    public bool IgnoreImage { get; set; }

    /// <summary>When true, the tag is a name (for example a person's), used to prefix the directory a wallpaper is saved under.</summary>
    public bool IsName { get; set; }

    /// <summary>When true, the tag is famous: it names a person or thing that users want their wallpapers kept apart for. Seeded from the person categories when the tag is first stored, then owned by the user.</summary>
    public bool IsFamous { get; set; }

    /// <summary>Navigation property to the files this tag is linked to.</summary>
    public ICollection<FileTagEntity> FileTags { get; } = [];
}
