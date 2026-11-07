using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<(string Category, LogLevel Level, EventId EventId)> entries = new();

    public IReadOnlyList<(string Category, LogLevel Level, EventId EventId)> Entries => [.. entries];

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, entries);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(string categoryName, ConcurrentQueue<(string Category, LogLevel Level, EventId EventId)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => entries.Enqueue((categoryName, logLevel, eventId));
    }
}
