using Serilog.Events;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

/// <summary>An immutable snapshot of a single Serilog log event, with PII already scrubbed.</summary>
public record LogEntry(DateTimeOffset Timestamp, LogEventLevel Level, string RenderedMessage, string? AccountId);

/// <summary>Factory for <see cref="LogEntry"/>.</summary>
public static class LogEntryFactory
{
    /// <summary>Creates a <see cref="LogEntry"/> with the supplied values.</summary>
    public static LogEntry Create(DateTimeOffset timestamp, LogEventLevel level, string renderedMessage, string? accountId)
    {
        ArgumentNullException.ThrowIfNull(renderedMessage);

        return new LogEntry(timestamp, level, renderedMessage, accountId);
    }
}
