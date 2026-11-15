namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the local log files for the log viewer (#1411).</summary>
public interface ILogEntryReader
{
    /// <summary>Streams the entries that match the query, oldest first, from every log file. A line that is not the start of an entry joins the entry before it. A file that cannot be read is skipped.</summary>
    /// <param name="query">What to find.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    IAsyncEnumerable<LogLine> ReadAsync(LogQuery query, CancellationToken cancellationToken);
}
