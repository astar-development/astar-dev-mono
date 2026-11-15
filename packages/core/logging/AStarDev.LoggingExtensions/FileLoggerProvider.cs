using System.Text;
using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingExtensions;

/// <summary>
/// Writes log entries to a local text file, one file per day named <c>{prefix}-{yyyyMMdd}.log</c> in the supplied directory, which is created on first write.
/// A failure to write is swallowed: losing a log line must never take the application down.
/// </summary>
/// <param name="directory">The directory the log files are written to.</param>
/// <param name="fileNamePrefix">The start of every log file name.</param>
/// <param name="timeProvider">The clock used for the entry timestamps and to choose the day's file.</param>
/// <param name="maxFileBytes">The size a day's file may reach before entries go to a new file, <c>{prefix}-{yyyyMMdd}-{n}.log</c>. Zero or less means no limit.</param>
/// <param name="admit">Decides, from an entry's level and the values of its log scopes, whether it is written. When it is <see langword="null"/> every entry is written.</param>
public sealed class FileLoggerProvider(string directory, string fileNamePrefix, TimeProvider timeProvider, Func<LogLevel, IReadOnlyDictionary<string, object?>, bool>? admit = null, long maxFileBytes = 0) : ILoggerProvider, ISupportExternalScope
{
    private readonly long fileLimit = maxFileBytes;
    private DateTime currentDay = DateTime.MinValue;
    private int currentIndex = 1;
    private readonly Func<LogLevel, IReadOnlyDictionary<string, object?>, bool>? admission = admit;
    private IExternalScopeProvider scopes = new LoggerExternalScopeProvider();

    /// <summary>The log scope key whose string value, the hashed account id, is written into the line so the log viewer can filter by account (#1411). No other scope value is ever written.</summary>
    public const string AccountIdHashKey = "AccountIdHash";

    private readonly Lock gate = new();

    /// <summary>Gets the path of the log file that entries written at <paramref name="moment"/> go to.</summary>
    /// <param name="moment">The time of the entry.</param>
    /// <returns>The full path of that day's log file.</returns>
    public string FilePathFor(DateTimeOffset moment) => Path.Combine(directory, $"{fileNamePrefix}-{moment:yyyyMMdd}.log");

    /// <inheritdoc/>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => scopes = scopeProvider;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to release: every entry is appended and closed straight away.
    }

    private Dictionary<string, object?> ScopeValues()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        scopes.ForEachScope(CollectScope, values);

        return values;
    }

    private bool Admits(LogLevel logLevel, IReadOnlyDictionary<string, object?> values) => logLevel != LogLevel.None && (admission is null || admission(logLevel, values));

    private static string AccountTag(IReadOnlyDictionary<string, object?> values) => values.TryGetValue(AccountIdHashKey, out var hash) && hash is string text && text.Length > 0 ? $" [{AccountIdHashKey}={text}]" : string.Empty;

    private static void CollectScope(object? scope, Dictionary<string, object?> values)
    {
        if (scope is not IEnumerable<KeyValuePair<string, object?>> pairs)
            return;

        foreach (var pair in pairs)
            values[pair.Key] = pair.Value;
    }

    private void Write(LogLevel logLevel, string categoryName, string message, Exception? exception, IReadOnlyDictionary<string, object?> values)
    {
        var now = timeProvider.GetLocalNow();
        var entry = new StringBuilder()
            .Append(CultureInvariant($"{now:yyyy-MM-dd HH:mm:ss.fff zzz} [{logLevel}]{AccountTag(values)} {categoryName}: {message}"))
            .AppendLine();
        if (exception is not null) _ = entry.AppendLine(exception.ToString());

        try
        {
            lock (gate)
            {
                _ = Directory.CreateDirectory(directory);
                File.AppendAllText(PathToWrite(now), entry.ToString());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Deliberately ignored - see the type summary.
        }
    }

    private string PathToWrite(DateTimeOffset moment)
    {
        if (fileLimit <= 0)
            return FilePathFor(moment);

        if (moment.Date != currentDay)
        {
            currentDay = moment.Date;
            currentIndex = 1;
        }

        var path = NumberedPath(moment, currentIndex);
        while (File.Exists(path) && new FileInfo(path).Length >= fileLimit)
            path = NumberedPath(moment, ++currentIndex);

        return path;
    }

    private string NumberedPath(DateTimeOffset moment, int index) => index == 1 ? FilePathFor(moment) : Path.Combine(directory, $"{fileNamePrefix}-{moment:yyyyMMdd}-{index}.log");

    private static string CultureInvariant(FormattableString value) => FormattableString.Invariant(value);

    private sealed class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var values = provider.ScopeValues();
            if (!provider.Admits(logLevel, values)) return;

            provider.Write(logLevel, categoryName, formatter(state, exception), exception, values);
        }
    }
}
