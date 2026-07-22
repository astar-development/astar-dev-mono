namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Generates a bounded, rounded-corner thumbnail from a downloaded wallpaper's full-size image bytes.
/// </summary>
public interface IWallpaperThumbnailGenerator
{
    /// <summary>
    ///     Produces a PNG-encoded thumbnail, scaled proportionally so neither dimension exceeds the
    ///     configured maximum, with rounded corners baked into the transparent background.
    /// </summary>
    /// <param name="imageBytes">The encoded full-size image bytes to generate a thumbnail from.</param>
    byte[] Generate(byte[] imageBytes);
}
