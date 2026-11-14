using AStarDev.Utilities;

namespace AStarDev.CloudSync.Logging;

/// <summary>Where CloudSync writes its local log files and how long it keeps them (#1411). Warnings and errors also go to Application Insights; Debug and Information stay in these files.</summary>
public static class CloudSyncLogging
{
    /// <summary>The start of every log file name, followed by the date.</summary>
    public const string FileNamePrefix = "cloudsync";

    /// <summary>How long a log file is kept.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    /// <summary>The folder for the log files: <c>logs</c> inside the application's data folder.</summary>
    public static string FolderPath() => "AStarDev.CloudSync".ApplicationDirectory().CombinePath("logs");
}
