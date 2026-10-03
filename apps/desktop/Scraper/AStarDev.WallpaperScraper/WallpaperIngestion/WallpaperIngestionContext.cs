using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>The per-page state a wallpaper ingestion needs, unchanged across every wallpaper on the page.</summary>
/// <param name="Output">Where and under what label the search's wallpapers are saved.</param>
/// <param name="Client">The HTTP client used to make requests.</param>
/// <param name="FileRepository">The repository used to store wallpapers' file entities.</param>
/// <param name="PersonCategories">The tag category names whose tags are famous when a tag has not been stored before.</param>
public sealed record WallpaperIngestionContext(SearchOutput Output, HttpClient Client, IRepository<FileEntity, FileId> FileRepository, IReadOnlyList<string> PersonCategories);
