namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Identifies which point in the scrape workflow an <see cref="IDelayStrategy" /> is being asked to delay for.</summary>
public enum DelayKind
{
    /// <summary>A search category was already up to date.</summary>
    CategoryUpToDate,

    /// <summary>Navigating between search-result pages.</summary>
    PageNavigation,

    /// <summary>An image was already downloaded.</summary>
    ImageAlreadyDownloaded,

    /// <summary>Pausing before visiting an individual image page.</summary>
    BeforeImage,

    /// <summary>Retrying a failed image page after a transient error.</summary>
    Retry,
}
