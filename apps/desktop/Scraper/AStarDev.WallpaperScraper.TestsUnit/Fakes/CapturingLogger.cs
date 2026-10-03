using Microsoft.Extensions.Logging;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

/// <summary>A logger that records what was logged so tests can assert on the outcome.</summary>
/// <typeparam name="T">The category the logger is for.</typeparam>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}
