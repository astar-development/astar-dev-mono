using AStarDev.ControlDb.TagDetail;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>An immutable snapshot of the flags of every tag stored when it was built. A tag in the snapshot owns its famous flag; a tag not in it is new.</summary>
public sealed record TagFlagCache
{
    private readonly Dictionary<int, TagFlagProjection> flagsById;

    private TagFlagCache(Dictionary<int, TagFlagProjection> flagsById) => this.flagsById = flagsById;

    /// <summary>A cache with no stored tags.</summary>
    public static TagFlagCache Empty { get; } = new([]);

    /// <summary>Builds a cache from the projected flags of the stored tags.</summary>
    /// <param name="flags">The flags of every stored tag.</param>
    public static TagFlagCache From(IEnumerable<TagFlagProjection> flags)
    {
        var flagsById = new Dictionary<int, TagFlagProjection>();
        foreach (var flag in flags) _ = flagsById.TryAdd(flag.WallhavenTagId, flag);

        return new TagFlagCache(flagsById);
    }

    /// <summary>Whether a tag with this Wallhaven id was stored when the cache was built.</summary>
    public bool IsStored(int wallhavenTagId) => flagsById.ContainsKey(wallhavenTagId);

    /// <summary>Whether the stored tag is flagged to ignore its images.</summary>
    public bool IsIgnored(int wallhavenTagId) => flagsById.TryGetValue(wallhavenTagId, out var flags) && flags.IgnoreImage;

    /// <summary>Whether the stored tag is flagged as a name.</summary>
    public bool IsName(int wallhavenTagId) => flagsById.TryGetValue(wallhavenTagId, out var flags) && flags.IsName;

    /// <summary>Whether the stored tag is flagged famous.</summary>
    public bool IsFamous(int wallhavenTagId) => flagsById.TryGetValue(wallhavenTagId, out var flags) && flags.IsFamous;
}
