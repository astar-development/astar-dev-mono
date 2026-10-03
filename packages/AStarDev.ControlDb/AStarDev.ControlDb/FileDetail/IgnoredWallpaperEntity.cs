namespace AStarDev.ControlDb.FileDetail;

/// <summary>A wallpaper the scraper chose not to keep because one of its tags is flagged to ignore images. Only the handle is stored, so later scrapes can skip it without fetching its tags again.</summary>
public sealed class IgnoredWallpaperEntity
{
    /// <summary>Primary key: the same handle a stored file would have had.</summary>
    public required FileHandle FileHandle { get; set; }
}
