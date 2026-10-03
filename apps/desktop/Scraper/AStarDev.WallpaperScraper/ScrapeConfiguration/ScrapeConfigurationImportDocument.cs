using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Serializable representation of the complete scrape configuration aggregate.</summary>
public sealed class ScrapeConfigurationImportDocument
{
    public Guid Id { get; init; }
    public UserConfigurationImportDocument UserConfiguration { get; init; } = new();
    public SearchConfigurationImportDocument SearchConfiguration { get; init; } = new();
    public ScrapeDirectoriesImportDocument ScrapeDirectories { get; init; } = new();
    public Uri BaseUrl { get; init; } = new("https://example.com");
    public string ApiKey { get; init; } = string.Empty;
    public string SearchString { get; init; } = string.Empty;
    public string TopWallpapers { get; init; } = string.Empty;
    public string HotWallpapers { get; init; } = "HotWallpapers";
    public string SearchStringPrefix { get; init; } = string.Empty;
    public string SearchStringSuffix { get; init; } = string.Empty;
    public string Subscriptions { get; init; } = string.Empty;
    public int ImagePauseInSeconds { get; init; }
    public int StartingPageNumber { get; init; }
    public int TotalPages { get; init; }
    public int SubscriptionsStartingPageNumber { get; init; }
    public int SubscriptionsTotalPages { get; init; }
    public int TopWallpapersStartingPageNumber { get; init; }
    public int TopWallpapersTotalPages { get; init; }
    public int HotWallpapersStartingPageNumber { get; init; }
    public int HotWallpapersTotalPages { get; init; }
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "The login page is free text and is not required to be a valid URL.")]
    public string LoginUrl { get; init; } = "https://example.com/login";
    public bool UseHeadless { get; init; }
    public float? SlowMotionDelay { get; init; }
}

public sealed class UserConfigurationImportDocument
{
    public Guid Id { get; init; }
    public string EmailAddress { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class SearchConfigurationImportDocument
{
    public Guid Id { get; init; }
    public string SearchTerm { get; init; } = string.Empty;
    public int? MaxResults { get; init; }
    public IReadOnlyList<SearchCategoryImportDocument> SearchCategories { get; init; } = [];
    public IReadOnlyList<string> PersonCategories { get; init; } = [.. PersonCategoryEntity.DefaultNames];
}

public sealed class SearchCategoryImportDocument
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int LastKnownImageCount { get; init; }
    public int LastPageVisited { get; init; }
    public int TotalPages { get; init; }
    public bool IncludeInSearch { get; init; } = true;
    public bool IsFamous { get; init; }
    public bool IsInternet { get; init; }
}

public sealed class ScrapeDirectoriesImportDocument
{
    public Guid Id { get; init; }
    public string RootDirectory { get; init; } = string.Empty;
    public string BaseSaveDirectory { get; init; } = string.Empty;
    public string BaseDirectory { get; init; } = string.Empty;
    public string RootDirectoryFamous { get; init; } = string.Empty;
    public string SubDirectoryName { get; init; } = string.Empty;
}
