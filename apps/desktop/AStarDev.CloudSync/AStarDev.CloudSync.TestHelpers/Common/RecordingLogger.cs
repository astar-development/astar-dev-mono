using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, Exception? Exception)> entries = [];
    private readonly List<string> messages = [];
    private readonly List<(string Name, IReadOnlyDictionary<string, object?> Values)> events = [];
    private readonly List<IReadOnlyDictionary<string, object?>> scopes = [];

    private readonly Lock gate = new();

    public IReadOnlyList<(LogLevel Level, Exception? Exception)> Entries => Snapshot(entries);

    public IReadOnlyList<string> Messages => Snapshot(messages);

    public IReadOnlyList<(string Name, IReadOnlyDictionary<string, object?> Values)> Events => Snapshot(events);

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Scopes => Snapshot(scopes);

    public IReadOnlyList<string> EventNames => [.. Events.Select(recorded => recorded.Name)];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var values = ToValues(state);
        lock(gate)
        {
            scopes.Add(values);
        }

        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var values = ToValues(state);
        lock(gate)
        {
            entries.Add((logLevel, exception));
            messages.Add(message);
            events.Add((eventId.Name ?? string.Empty, values));
        }
    }

    public IReadOnlyDictionary<string, object?> ValuesOf(string eventName) => Events.Last(recorded => recorded.Name == eventName).Values;

    private T2[] Snapshot<T2>(List<T2> source)
    {
        lock(gate)
        {
            return [.. source];
        }
    }

    private static Dictionary<string, object?> ToValues<TState>(TState state) =>
        state is IEnumerable<KeyValuePair<string, object?>> pairs ? pairs.GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.Last().Value) : [];
}
