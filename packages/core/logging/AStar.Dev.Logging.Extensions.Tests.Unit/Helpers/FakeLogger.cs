namespace AStar.Dev.Logging.Extensions.Tests.Unit.Helpers;

internal sealed class FakeLogger : ILogger
{
    public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Logs { get; } = [];

    public IDisposable BeginScope<TState>(TState? state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        Logs.Add((logLevel, eventId, message, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}
