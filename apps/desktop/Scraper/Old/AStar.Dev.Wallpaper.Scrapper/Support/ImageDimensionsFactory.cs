namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Factory methods for creating instances of <see cref="ImageDimensions" />.</summary>
public static class ImageDimensionsFactory
{
    /// <summary>Creates an <see cref="ImageDimensions" /> for the given <paramref name="width" /> and <paramref name="height" />.</summary>
    public static ImageDimensions Create(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        return new(width, height);
    }
}
