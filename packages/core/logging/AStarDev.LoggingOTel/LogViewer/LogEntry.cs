using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingOTel.LogViewer;

/// <summary>An immutable snapshot of a single OpenTelemetry log record, with PII already scrubbed.</summary>
public record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string RenderedMessage);

/// <summary>Factory for <see cref="LogEntry"/>.</summary>
public static class LogEntryFactory
{
    /// <summary>Creates a <see cref="LogEntry"/> with the supplied values.</summary>
    public static LogEntry Create(DateTimeOffset timestamp, LogLevel level, string renderedMessage)
        => new(timestamp, level, renderedMessage);
}
