using AStar.Dev.Wallpaper.Scrapper.Repositories;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>The outcome of scraping an image page, replacing the historical <c>ImagePageResult</c>.</summary>
public abstract record ImagePageOutcome;

/// <summary>The image page's tags were accepted; the image should be downloaded and saved.</summary>
/// <param name="ImageUrl">The URL of the image to download.</param>
/// <param name="DirectorySegments">The directory segments the image should be saved under.</param>
/// <param name="FilePrefix">The prefix to apply to the saved file name.</param>
/// <param name="Tags">The tag text scraped from the image page.</param>
/// <param name="RawTags">The raw tag data scraped from the image page.</param>
public sealed record ScrapedImage(string ImageUrl, IReadOnlyList<string> DirectorySegments, string FilePrefix, IReadOnlyList<string> Tags, IReadOnlyList<TagData> RawTags) : ImagePageOutcome;

/// <summary>The image page's tags matched an ignore rule; the image should not be downloaded.</summary>
/// <param name="Tags">The tag text scraped from the image page before the skip was detected.</param>
/// <param name="RawTags">The raw tag data scraped from the image page before the skip was detected.</param>
public sealed record SkippedImage(IReadOnlyList<string> Tags, IReadOnlyList<TagData> RawTags) : ImagePageOutcome;
