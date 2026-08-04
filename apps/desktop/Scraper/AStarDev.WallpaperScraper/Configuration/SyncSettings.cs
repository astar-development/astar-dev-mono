using System.ComponentModel.DataAnnotations;

namespace AStarDev.WallpaperScraper.Configuration;

/// <summary>
///   Represents the synchronization settings for the application, including the progress report interval.
/// </summary>
public record SyncSettings()
{
    /// <summary>
    ///   The name of the configuration section that contains the sync settings.
    /// </summary>
    public const string SectionName = "SyncSettings";

    /// <summary>
    /// How many files must complete before a progress event is dispatched to the UI.
    /// Applies to both the file-sync phase and the enumeration phase.
    /// Must be at least 1.
    /// </summary>
    [Range(1, 500, ErrorMessage = "ProgressReportInterval must be at least 1.")]
    public int ProgressReportInterval { get; set; }

    /// <summary>
    ///  Gets or sets the initial size of the application window.
    /// </summary>
    public WindowSize WindowSize { get; set; } = new WindowSize(800, 600);
}
