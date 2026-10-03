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
public sealed class FileLoggerProvider(string directory, string fileNamePrefix, TimeProvider timeProvider) : ILoggerProvider
{
    private readonly Lock gate = new();

    /// <summary>Gets the path of the log file that entries written at <paramref name="moment"/> go to.</summary>
    /// <param name="moment">The time of the entry.</param>
    /// <returns>The full path of that day's log file.</returns>
    public string FilePathFor(DateTimeOffset moment) => Path.Combine(directory, $"{fileNamePrefix}-{moment:yyyyMMdd}.log");

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to release: every entry is appended and closed straight away.
    }

    private void Write(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        var now = timeProvider.GetLocalNow();
        var entry = new StringBuilder()
            .Append(CultureInvariant($"{now:yyyy-MM-dd HH:mm:ss.fff zzz} [{logLevel}] {categoryName}: {message}"))
            .AppendLine();
        if (exception is not null) _ = entry.AppendLine(exception.ToString());

        try
        {
            lock (gate)
            {
                _ = Directory.CreateDirectory(directory);
                File.AppendAllText(FilePathFor(now), entry.ToString());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Deliberately ignored - see the type summary.
        }
    }

    private static string CultureInvariant(FormattableString value) => FormattableString.Invariant(value);

    private sealed class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            provider.Write(logLevel, categoryName, formatter(state, exception), exception);
        }
    }
}
