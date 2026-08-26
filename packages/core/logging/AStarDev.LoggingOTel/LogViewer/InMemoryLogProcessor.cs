using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace AStarDev.LoggingOTel.LogViewer;

/// <summary>
///     OpenTelemetry log processor that retains the last <see cref="DefaultCapacity"/> log entries in a thread-safe ring buffer
///     and exposes them via <see cref="ILogEntryProvider"/> (LG-01, NF-07).
///     PII (email addresses) is scrubbed before storage.
/// </summary>
public sealed class InMemoryLogProcessor : BaseProcessor<LogRecord>, ILogEntryProvider
{
    /// <summary>Default maximum number of log entries held in memory.</summary>
    public const int DefaultCapacity = 500;

    private readonly int capacity;
    private readonly ConcurrentQueue<LogEntry> entries = new();
    private readonly Subject<LogEntry> subject = new();
    private bool disposed;

    /// <summary>Initialises the processor with <see cref="DefaultCapacity"/>.</summary>
    public InMemoryLogProcessor() : this(DefaultCapacity) { }

    private InMemoryLogProcessor(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        this.capacity = capacity;
    }

    /// <inheritdoc />
    public IObservable<LogEntry> EntryAdded => subject;

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetSnapshot() => [.. entries];

    /// <summary>Called by the OpenTelemetry logging pipeline on arbitrary threads. Never blocks.</summary>
    public override void OnEnd(LogRecord data)
    {
        var entry = ToLogEntry(data);
        entries.Enqueue(entry);

        while (entries.Count > capacity)
            entries.TryDequeue(out _);

        subject.OnNext(entry);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            disposed = true;
            subject.OnCompleted();
            subject.Dispose();
        }

        base.Dispose(disposing);
    }

    private static LogEntry ToLogEntry(LogRecord logRecord)
    {
        string rendered = PiiScrubber.Scrub(logRecord.FormattedMessage ?? string.Empty);
        string? accountId = ExtractAccountId(logRecord);
        var timestamp = new DateTimeOffset(logRecord.Timestamp, TimeSpan.Zero);

        return LogEntryFactory.Create(timestamp, logRecord.LogLevel, rendered, accountId);
    }

    private static string? ExtractAccountId(LogRecord logRecord)
    {
        if (logRecord.Attributes is null)
            return null;

        foreach (var attribute in logRecord.Attributes)
        {
            if (attribute.Key == "AccountId")
                return attribute.Value?.ToString();
        }

        return null;
    }
}
