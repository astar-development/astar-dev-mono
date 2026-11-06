namespace AStarDev.LoggingExtensions.TestsUnit.Helpers;

internal sealed class FakeLogger<TCategoryName> : ILogger<TCategoryName>
{
    private readonly FakeLogger inner = new();

    public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Logs => inner.Logs;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => inner.Log(logLevel, eventId, state, exception, formatter);
}
