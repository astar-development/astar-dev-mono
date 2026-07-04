namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Builds the single on-disk filename for a scraped image so the saved file and its FileDetail row can never diverge.</summary>
public static class ScrapedFileNameFactory
{
    /// <summary>Creates the lowercased filename, prefixed with <paramref name="filePrefix"/> when supplied, from the final segment of <paramref name="imageUrl"/>.</summary>
    public static string Create(string? filePrefix, string imageUrl)
    {
        string filename = Path.GetFileName(imageUrl).ToLowerInvariant();

        return string.IsNullOrEmpty(filePrefix) ? filename : $"{filePrefix} {filename}".ToLowerInvariant();
    }
}
