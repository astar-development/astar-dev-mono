namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>A decoded, display-ready wallpaper image together with the details to show alongside it.</summary>
/// <param name="PngStream">A stream, positioned at the start, containing the PNG-encoded image.</param>
/// <param name="Info">The wallpaper's display details.</param>
public sealed record WallpaperPreviewImage(Stream PngStream, WallpaperInfo Info);
