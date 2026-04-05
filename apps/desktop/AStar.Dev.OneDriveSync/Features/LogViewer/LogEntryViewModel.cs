using System;
using AStar.Dev.OneDriveSync.Infrastructure;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

/// <summary>Display wrapper for a single <see cref="LogEntry"/> row in the Log Viewer list (S014).</summary>
public sealed class LogEntryViewModel : ViewModelBase
{
    /// <summary>Initialises a new view model from the given <paramref name="entry"/>.</summary>
    public LogEntryViewModel(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Timestamp       = entry.Timestamp;
        Level           = entry.Level;
        LevelDisplay    = entry.Level.ToString();
        RenderedMessage = entry.RenderedMessage;
        AccountId       = entry.AccountId ?? string.Empty;
    }

    /// <summary>UTC timestamp of the log event.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Serilog severity level.</summary>
    public LogEventLevel Level { get; }

    /// <summary>Display string for <see cref="Level"/>.</summary>
    public string LevelDisplay { get; }

    /// <summary>PII-scrubbed rendered log message.</summary>
    public string RenderedMessage { get; }

    /// <summary>Synthetic account identifier, or empty string when the entry is not account-scoped.</summary>
    public string AccountId { get; }
}
