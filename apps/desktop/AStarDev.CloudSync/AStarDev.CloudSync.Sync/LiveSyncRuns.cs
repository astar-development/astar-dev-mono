using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The journal rows of runs this app session started and has not finished (#1632). Startup recovery skips them so a run in flight is never marked interrupted.</summary>
internal sealed class LiveSyncRuns : IDisposable
{
    private readonly ConcurrentDictionary<SyncRunId, byte> live = new();
    private readonly SemaphoreSlim starts = new(1, 1);

    /// <summary>Runs the journal write that starts a run, and registers the run in the same step, so startup recovery can never see the row without the registration (#1660).</summary>
    /// <param name="start">Writes the row and, when it was written, calls <see cref="Add"/> for it.</param>
    public async Task<Option<SyncRunId>> StartingAsync(Func<Task<Option<SyncRunId>>> start)
    {
        await starts.WaitAsync().ConfigureAwait(false);
        try
        {
            return await start().ConfigureAwait(false);
        }
        finally
        {
            _ = starts.Release();
        }
    }

    /// <summary>Reads while no run can start, and returns what was read with the runs live at that moment. A run that finishes later is still in the returned set, so recovery can never mark a run this session finished as interrupted (#1660).</summary>
    /// <typeparam name="T">What the read returns.</typeparam>
    /// <param name="read">The read, for example the journal's unfinished rows.</param>
    /// <param name="cancellationToken">Cancels the wait for starting runs.</param>
    public async Task<(T Value, IReadOnlySet<SyncRunId> Live)> ReadingAsync<T>(Func<Task<T>> read, CancellationToken cancellationToken)
    {
        await starts.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshot = live.Keys.ToHashSet();

            return (await read().ConfigureAwait(false), snapshot);
        }
        finally
        {
            _ = starts.Release();
        }
    }

    /// <summary>Notes that a run's row is being worked on.</summary>
    /// <param name="runId">The run.</param>
    public void Add(SyncRunId runId) => live[runId] = 0;

    /// <summary>Notes that a run's row is finished.</summary>
    /// <param name="runId">The run.</param>
    public void Remove(SyncRunId runId) => _ = live.TryRemove(runId, out _);

    /// <inheritdoc/>
    public void Dispose() => starts.Dispose();
}
