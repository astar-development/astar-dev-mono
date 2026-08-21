namespace AStarDev.WallpaperScraper.Services;

/// <summary>
/// Represents an exception that occurs during an operation in the wallpaper scraper service.
/// </summary>
/// <param name="errorMessage">The error message describing the exception.</param>
internal sealed class OperationException(string errorMessage) : Exception(errorMessage);
