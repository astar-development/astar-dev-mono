using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Subjects;
using Serilog.Core;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

/// <summary>
///     Serilog sink that retains the last <see cref="DefaultCapacity"/> log entries in a thread-safe ring buffer
///     and exposes them via <see cref="ILogEntryProvider"/> (LG-01, NF-07).
///     PII (email addresses) is scrubbed before storage.
/// </summary>
public sealed class InMemoryLogSink : ILogEventSink, ILogEntryProvider, IDisposable
{
    /// <summary>Default maximum number of log entries held in memory.</summary>
    public const int DefaultCapacity = 500;

    private readonly int _capacity;
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly Subject<LogEntry> _subject = new();

    /// <summary>Initialises the sink with <see cref="DefaultCapacity"/>.</summary>
    public InMemoryLogSink() : this(DefaultCapacity) { }

    /// <summary>Initialises the sink with a custom <paramref name="capacity"/>. Exposed internal for testing.</summary>
    internal InMemoryLogSink(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    /// <inheritdoc />
    public IObservable<LogEntry> EntryAdded => _subject;

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetSnapshot() => [.._entries];

    /// <summary>Called by the Serilog pipeline on arbitrary threads. Never blocks.</summary>
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var entry = ToLogEntry(logEvent);
        _entries.Enqueue(entry);

        while (_entries.Count > _capacity)
            _entries.TryDequeue(out _);

        _subject.OnNext(entry);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }

    private static LogEntry ToLogEntry(LogEvent logEvent)
    {
        string rendered  = PiiScrubber.Scrub(logEvent.RenderMessage(CultureInfo.InvariantCulture));
        string? accountId = ExtractAccountId(logEvent);

        return LogEntryFactory.Create(logEvent.Timestamp, logEvent.Level, rendered, accountId);
    }

    private static string? ExtractAccountId(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("AccountId", out var property))
        {

            return null;
        }

        string raw = property.ToString();

        return raw.Trim('"');
    }
}
