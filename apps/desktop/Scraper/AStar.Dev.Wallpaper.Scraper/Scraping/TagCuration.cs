namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents the outcome of filtering a wallpaper's raw tags against the ignore lists.
/// </summary>
/// <param name="Kept">The tags that survived filtering and should be saved.</param>
/// <param name="Messages">
/// Human-readable messages explaining what was kept or ignored, and why.
/// This can be used to provide feedback to the user or for debugging purposes.
/// </param>
public sealed record TagCuration(IReadOnlyList<TagData> Kept, IReadOnlyList<string> Messages);
