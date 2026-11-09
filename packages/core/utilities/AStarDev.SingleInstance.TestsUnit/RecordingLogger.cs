using Microsoft.Extensions.Logging;

namespace AStarDev.SingleInstance.TestsUnit;

internal sealed class RecordingLogger : ILogger, ILoggerProvider
{
    private readonly Lock gate = new();
    private readonly List<(LogLevel Level, string Message)> entries = [];

    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (gate)
                return [.. entries];
        }
    }

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        lock (gate)
            entries.Add((logLevel, formatter(state, exception)));
    }

    public void Dispose()
    {
    }
}
