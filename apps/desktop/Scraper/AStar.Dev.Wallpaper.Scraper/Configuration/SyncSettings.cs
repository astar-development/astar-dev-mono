using System.ComponentModel.DataAnnotations;

namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
///     Runtime-configurable settings for the sync pipeline.
///     Mutable to support direct <see cref="IConfiguration" /> binding; not a candidate for the Records rule.
/// </summary>
public class SyncSettings
{
    internal static string SectionName => "Sync";

    /// <summary>
    /// How many files must complete before a progress event is dispatched to the UI.
    /// Applies to both the file-sync phase and the enumeration phase.
    /// Must be at least 1.
    /// </summary>
    [Range(1, 500, ErrorMessage = "ProgressReportInterval must be at least 1.")]
    public int ProgressReportInterval { get; set; }
}
