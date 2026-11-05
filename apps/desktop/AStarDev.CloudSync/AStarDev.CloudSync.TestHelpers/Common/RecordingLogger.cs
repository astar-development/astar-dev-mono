using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, Exception? Exception)> entries = [];
    private readonly List<string> messages = [];
    private readonly List<(string Name, IReadOnlyDictionary<string, object?> Values)> events = [];
    private readonly List<IReadOnlyDictionary<string, object?>> scopes = [];

    public IReadOnlyList<(LogLevel Level, Exception? Exception)> Entries => entries;

    public IReadOnlyList<string> Messages => messages;

    public IReadOnlyList<(string Name, IReadOnlyDictionary<string, object?> Values)> Events => events;

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Scopes => scopes;

    public IReadOnlyList<string> EventNames => [.. events.Select(recorded => recorded.Name)];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        scopes.Add(ToValues(state));

        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        entries.Add((logLevel, exception));
        messages.Add(formatter(state, exception));
        events.Add((eventId.Name ?? string.Empty, ToValues(state)));
    }

    public IReadOnlyDictionary<string, object?> ValuesOf(string eventName) => events.Last(recorded => recorded.Name == eventName).Values;

    private static Dictionary<string, object?> ToValues<TState>(TState state) =>
        state is IEnumerable<KeyValuePair<string, object?>> pairs ? pairs.GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.Last().Value) : [];
}
