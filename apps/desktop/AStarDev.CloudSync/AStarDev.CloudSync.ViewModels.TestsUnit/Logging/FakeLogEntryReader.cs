using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Logging;

public sealed class FakeLogEntryReader : ILogEntryReader
{
    public List<LogLine> Lines { get; } = [];

    public int Reads { get; private set; }

    public async IAsyncEnumerable<LogLine> ReadAsync(LogQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Reads++;
        await Task.Yield();
        foreach (var line in Lines.Where(query.Matches))
            yield return line;
    }
}
