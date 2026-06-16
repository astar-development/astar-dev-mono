using System.ComponentModel.DataAnnotations;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

/// <summary>Runtime-configurable settings for the sync pipeline.</summary>
public record SyncSettings
{
    internal static string SectionName => "Sync";

    /// <summary>
    /// How many files must complete before a progress event is dispatched to the UI.
    /// Applies to both the file-sync phase and the enumeration phase.
    /// Must be at least 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ProgressReportInterval must be at least 1.")]
    public required int ProgressReportInterval { get; init; }

    /// <summary>Maximum number of parallel download workers. Must be between 1 and 8.</summary>
    [Range(1, 8, ErrorMessage = "MaxConcurrentDownloads must be between 1 and 8.")]
    public required int MaxConcurrentDownloads { get; init; }
}
