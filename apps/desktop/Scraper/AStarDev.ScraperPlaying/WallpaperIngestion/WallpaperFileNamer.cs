using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Generates the file name a wallpaper is saved and recorded under.</summary>
public static class WallpaperFileNamer
{
    private const int MaxPrefixLength = 100;

    /// <summary>Generates the file name for a wallpaper. Wallpapers with famous tags (those flagged <c>IsFamous</c>) are prefixed with the tag names, joined by underscores; all others use just the wallpaper id.</summary>
    /// <param name="wallpaperId">Wallhaven's id for the wallpaper.</param>
    /// <param name="extension">The file extension, including the leading '.'.</param>
    /// <param name="tags">The wallpaper's tags.</param>
    /// <returns>The file name.</returns>
    public static FileName Create(string wallpaperId, string extension, IReadOnlyList<Tag> tags)
    {
        var prefix = CreatePrefix(tags);
        var baseName = prefix.Length == 0 ? wallpaperId : $"{prefix}_{wallpaperId}";

        return new FileName($"{baseName}{extension}");
    }

    private static string CreatePrefix(IReadOnlyList<Tag> tags)
    {
        var joined = string.Join('_', tags.Where(tag => tag.IsFamous).Select(tag => Sanitise(tag.Name)).Where(name => name.Length > 0));

        return joined.TruncateIfRequired(MaxPrefixLength);
    }

    private static string Sanitise(string tagName)
        => tagName.Trim().Replace(' ', '_').RemoveInvalidFileNameCharacters();
}
