namespace AStar.Dev.Wallpaper.Scrapper;

public static class ScrapperConstants
{
    public const int ImagesPerPage = 24;

    public const int ThumbnailSize = 500;

    public const float ThumbnailCornerRadius = 20f;

    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan ImageAlreadyDownloadedDelay = TimeSpan.FromMilliseconds(500);
}
