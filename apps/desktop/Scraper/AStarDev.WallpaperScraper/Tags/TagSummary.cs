using AStarDev.ControlDb.TagDetail;

namespace AStarDev.WallpaperScraper.Tags;

/// <summary>The details of a stored tag shown in the tags editor.</summary>
/// <param name="WallhavenTagId">Wallhaven's own id for the tag, the key the editor saves changes by.</param>
/// <param name="Name">The tag's display name.</param>
/// <param name="Category">The tag's category name.</param>
/// <param name="Purity">The tag's purity rating.</param>
/// <param name="IgnoreImage">Whether any image carrying the tag is ignored.</param>
/// <param name="IsName">Whether the tag is a name.</param>
/// <param name="IsFamous">Whether the tag is famous.</param>
public sealed record TagSummary(int WallhavenTagId, string Name, string Category, string Purity, bool IgnoreImage, bool IsName, bool IsFamous)
{
    /// <summary>Creates a summary of the specified tag.</summary>
    /// <param name="entity">The tag to summarise.</param>
    public static TagSummary From(TagEntity entity) => new(entity.WallhavenTagId, entity.Name, entity.Category, entity.Purity, entity.IgnoreImage, entity.IsName, entity.IsFamous);
}
