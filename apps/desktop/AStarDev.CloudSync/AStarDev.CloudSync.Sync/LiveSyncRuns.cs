using System.Collections.Concurrent;

namespace AStarDev.CloudSync.Sync;

/// <summary>The journal rows of runs this app session started and has not finished (#1632). Startup recovery skips them so a run in flight is never marked interrupted.</summary>
internal sealed class LiveSyncRuns
{
    private readonly ConcurrentDictionary<SyncRunId, byte> live = new();

    /// <summary>Notes that a run's row is being worked on.</summary>
    /// <param name="runId">The run.</param>
    public void Add(SyncRunId runId) => live[runId] = 0;

    /// <summary>Notes that a run's row is finished.</summary>
    /// <param name="runId">The run.</param>
    public void Remove(SyncRunId runId) => _ = live.TryRemove(runId, out _);

    /// <summary>Whether this session is still working on the run.</summary>
    /// <param name="runId">The run.</param>
    public bool Contains(SyncRunId runId) => live.ContainsKey(runId);
}
