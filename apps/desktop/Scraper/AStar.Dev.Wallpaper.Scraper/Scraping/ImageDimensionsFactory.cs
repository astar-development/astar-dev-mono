namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Factory for <see cref="ImageDimensions"/>.
/// </summary>
public static class ImageDimensionsFactory
{
    /// <summary>
    ///     Creates an <see cref="ImageDimensions"/> from a decoded image's width and height. Widths and
    ///     heights come from decoding externally-sourced, unvalidated image bytes, so negative values
    ///     (which a malformed or adversarial image could otherwise produce) are normalized to zero rather
    ///     than propagated.
    /// </summary>
    /// <param name="width">The decoded image width, in pixels.</param>
    /// <param name="height">The decoded image height, in pixels.</param>
    public static ImageDimensions Create(int width, int height) =>
        new(Math.Max(width, 0), Math.Max(height, 0));
}
