using System.Collections.Concurrent;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.old.Logging;

/// <summary>
/// Manages Serilog configuration for the desktop app, including per-account log-level
/// switches (LG-01) and platform-appropriate file paths (LG-05).
/// </summary>
public sealed class LoggingService : IDisposable
{
    private readonly ConcurrentDictionary<string, LoggingLevelSwitch> _accountSwitches = new();

    public InMemoryLogSink Sink { get; } = new();

    public string LogDirectory { get; }

    public LoggingService()
    {
        LogDirectory = GetPlatformLogDirectory();
        _ = Directory.CreateDirectory(LogDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(Sink)
            .WriteTo.File(new SanitisedTextFormatter(), Path.Combine(LogDirectory, "onedrive-sync-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
    }

    public void SetAccountDebugEnabled(string accountId, bool enabled)
    {
        LoggingLevelSwitch sw = _accountSwitches.GetOrAdd(accountId, _ => new LoggingLevelSwitch(LogEventLevel.Warning));
        sw.MinimumLevel = enabled ? LogEventLevel.Verbose : LogEventLevel.Warning;
    }

    public bool IsAccountDebugEnabled(string accountId) => _accountSwitches.TryGetValue(accountId, out LoggingLevelSwitch? sw) && sw.MinimumLevel <= LogEventLevel.Debug;

    public void Dispose() => Log.CloseAndFlush();

    private static string GetPlatformLogDirectory()
    {
        if (OperatingSystem.IsLinux())
        {
            var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var basePath = string.IsNullOrWhiteSpace(xdgData) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share") : xdgData;
            return Path.Combine(basePath, "AStar.Dev.OneDriveSync.old", "logs");
        }

        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "AStar.Dev.OneDriveSync.old");

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AStar.Dev.OneDriveSync.old", "logs");
    }
}
