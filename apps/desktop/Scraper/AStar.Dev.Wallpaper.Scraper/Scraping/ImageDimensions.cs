namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents the pixel dimensions of a decoded image.
/// </summary>
/// <param name="Width">The image width, in pixels.</param>
/// <param name="Height">The image height, in pixels.</param>
public sealed record ImageDimensions(int Width, int Height);
