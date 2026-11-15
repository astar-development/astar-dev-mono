using AStarDev.CloudSync.Sync;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace AStarDev.CloudSync.Logging;

/// <summary>Where CloudSync writes its local log files and how long it keeps them (#1411). Warnings and errors also go to Application Insights; Debug and Information stay in these files.</summary>
public static class CloudSyncLogging
{
    /// <summary>The start of every log file name, followed by the date.</summary>
    public const string FileNamePrefix = "cloudsync";

    /// <summary>How long a log file is kept.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    /// <summary>The size one log file may reach before the day's entries go to a new file: 10 MB.</summary>
    public const long MaxFileBytes = 10L * 1024 * 1024;

    /// <summary>The most the log files may take up together: 100 MB.</summary>
    public const long MaxTotalBytes = 100L * 1024 * 1024;

    /// <summary>The folder for the log files on this platform (see <see cref="LogFolderResolver"/>).</summary>
    public static string FolderPath() => LogFolderResolver.Resolve(LogEnvironmentFactory.CreateCurrent());

    /// <summary>Sets up what goes where. Every entry is offered to the local file, which writes an account's entries below warning level only when that account is verbose (<see cref="AccountLogLevelFilter"/>). Application Insights gets warnings and above only, so verbose output stays on the machine.</summary>
    /// <param name="logging">The logging builder. It already has the OpenTelemetry provider.</param>
    /// <param name="levels">The accounts that log verbosely.</param>
    /// <param name="folder">The folder for the log files.</param>
    public static ILoggingBuilder Configure(ILoggingBuilder logging, AccountLogLevels levels, string folder) =>
        logging.SetMinimumLevel(LogLevel.Trace)
            .AddFilter<OpenTelemetryLoggerProvider>(level => level >= LogLevel.Warning)
            .AddFileLogging(folder, FileNamePrefix, (level, scope) => AccountLogLevelFilter.Admits(levels, level, scope), MaxFileBytes);
}
