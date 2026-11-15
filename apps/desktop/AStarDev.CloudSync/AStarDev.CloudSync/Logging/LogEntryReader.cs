using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Logging;

/// <summary>Reads the app's local log files (#1411).</summary>
/// <param name="locator">Says where the files are.</param>
public sealed class LogEntryReader(ILogFileLocator locator) : ILogEntryReader
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<LogLine> ReadAsync(LogQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var folder = locator.GetLogFolder();
        folder.Refresh();
        if (!folder.Exists)
            yield break;

        foreach (var file in folder.EnumerateFiles($"{CloudSyncLogging.FileNamePrefix}-*.log").OrderBy(file => DayOf(file.Name), StringComparer.Ordinal).ThenBy(file => IndexOf(file.Name)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var reader = new StreamReader(Open(file));
            await foreach (var line in EntriesAsync(reader, cancellationToken).ConfigureAwait(false))
                if (query.Matches(line))
                    yield return line;
        }
    }

    private static string DayOf(string name) => Segments(name).ElementAtOrDefault(1) ?? string.Empty;

    private static int IndexOf(string name) => int.TryParse(Segments(name).ElementAtOrDefault(2), out var index) ? index : 1;

    private static string[] Segments(string name) => Path.GetFileNameWithoutExtension(name).Split('-');

    private static Stream Open(IFileInfo file) => Try.Run(() => file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).Match(stream => stream, _ => Stream.Null);

    private static async IAsyncEnumerable<LogLine> EntriesAsync(StreamReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LogLine? pending = null;
        string? text;
        while ((text = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            var started = LogLineParser.Parse(text).Match(line => line, () => null!);
            if (started is not null)
            {
                if (pending is not null)
                    yield return pending;
                pending = started;
            }
            else if (pending is not null)
                pending = pending with { Message = $"{pending.Message}\n{text}" };
        }

        if (pending is not null)
            yield return pending;
    }
}
