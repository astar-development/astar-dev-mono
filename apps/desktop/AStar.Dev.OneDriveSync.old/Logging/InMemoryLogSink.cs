using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace AStar.Dev.OneDriveSync.old.Logging;

/// <summary>
/// Serilog sink that buffers log events in memory for display in the log viewer UI (LG-04).
/// </summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();
    private readonly int _maxCapacity;

    public InMemoryLogSink(int maxCapacity = 5_000) => _maxCapacity = maxCapacity;

    public event Action<LogEvent>? LogEventReceived;

    public void Emit(LogEvent logEvent)
    {
        _events.Enqueue(logEvent);
        while (_events.Count > _maxCapacity)
            _ = _events.TryDequeue(out _);

        LogEventReceived?.Invoke(logEvent);
    }

    public void Clear()
    {
        while (_events.TryDequeue(out _)) { }
    }
}
