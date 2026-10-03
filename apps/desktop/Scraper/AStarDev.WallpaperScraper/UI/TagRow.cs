using AStarDev.WallpaperScraper.Tags;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>The editable state of one tag row in <see cref="TagsEditorWindow"/>; only the flags can be changed.</summary>
/// <param name="summary">The stored tag the row starts from.</param>
public sealed class TagRow(TagSummary summary)
{
    /// <summary>Wallhaven's id for the tag.</summary>
    public int WallhavenTagId { get; } = summary.WallhavenTagId;

    /// <summary>The tag's display name.</summary>
    public string Name { get; } = summary.Name;

    /// <summary>The tag's category name.</summary>
    public string Category { get; } = summary.Category;

    /// <summary>The tag's purity rating.</summary>
    public string Purity { get; } = summary.Purity;

    /// <summary>Whether any image carrying the tag is ignored.</summary>
    public bool IgnoreImage { get; set; } = summary.IgnoreImage;

    /// <summary>Whether the tag is a name, used to prefix the directory a wallpaper is saved under.</summary>
    public bool IsName { get; set; } = summary.IsName;

    /// <summary>Whether the tag is famous.</summary>
    public bool IsFamous { get; set; } = summary.IsFamous;

    /// <summary>The flags currently set on the row.</summary>
    public TagFlags Flags => new(IgnoreImage, IsName, IsFamous);

    /// <summary>Whether any flag differs from the stored tag.</summary>
    public bool IsChanged => IgnoreImage != summary.IgnoreImage || IsName != summary.IsName || IsFamous != summary.IsFamous;

    /// <summary>Whether the row's name or category contains <paramref name="filter"/>, ignoring case; every row matches an empty filter.</summary>
    /// <param name="filter">The text to look for.</param>
    public bool Matches(string filter) =>
        Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || Category.Contains(filter, StringComparison.OrdinalIgnoreCase);
}
