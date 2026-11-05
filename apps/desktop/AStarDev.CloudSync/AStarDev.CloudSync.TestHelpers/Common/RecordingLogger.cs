using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, Exception? Exception)> entries = [];
    private readonly List<string> messages = [];

    public IReadOnlyList<(LogLevel Level, Exception? Exception)> Entries => entries;

    public IReadOnlyList<string> Messages => messages;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        entries.Add((logLevel, exception));
        messages.Add(formatter(state, exception));
    }
}
