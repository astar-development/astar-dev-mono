using System;
using System.Collections.Generic;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

/// <summary>Provides access to in-memory log entries captured by <see cref="InMemoryLogSink"/>.</summary>
public interface ILogEntryProvider
{
    /// <summary>Returns a point-in-time snapshot of all retained log entries.</summary>
    IReadOnlyList<LogEntry> GetSnapshot();

    /// <summary>Observable that fires each time a new <see cref="LogEntry"/> is captured.</summary>
    IObservable<LogEntry> EntryAdded { get; }
}
