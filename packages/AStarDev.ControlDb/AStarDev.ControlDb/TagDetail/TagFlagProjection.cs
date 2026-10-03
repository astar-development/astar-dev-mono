namespace AStarDev.ControlDb.TagDetail;

/// <summary>The flags of one stored tag, projected without loading the whole <see cref="TagEntity"/>.</summary>
/// <param name="WallhavenTagId">The Wallhaven id of the tag.</param>
/// <param name="IgnoreImage">Whether the tag is flagged <see cref="TagEntity.IgnoreImage"/>.</param>
/// <param name="IsName">Whether the tag is flagged <see cref="TagEntity.IsName"/>.</param>
/// <param name="IsFamous">Whether the tag is flagged <see cref="TagEntity.IsFamous"/>.</param>
public readonly record struct TagFlagProjection(int WallhavenTagId, bool IgnoreImage, bool IsName, bool IsFamous);
