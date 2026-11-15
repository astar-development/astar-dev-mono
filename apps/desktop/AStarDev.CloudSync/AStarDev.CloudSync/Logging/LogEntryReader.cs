using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Logging;

/// <summary>Reads the app's local log files (#1411).</summary>
/// <param name="locator">Says where the files are.</param>
public sealed class LogEntryReader(ILogFileLocator locator) : ILogEntryReader
{
    /// <inheritdoc/>
    public IAsyncEnumerable<LogLine> ReadAsync(LogQuery query, CancellationToken cancellationToken) => throw new NotImplementedException($"{locator}{query}{cancellationToken}");
}
