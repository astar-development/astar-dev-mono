namespace AStarDev.CloudSync.Sync;

/// <summary>Collects the baseline rows one stage of the download pass writes, so they go to the database in one batch instead of one call per file (#1400). Safe for the parallel downloads.</summary>
internal sealed class BaselineBatch
{
    private readonly Lock gate = new();
    private readonly List<SyncBaselineEntry> upserts = [];
    private readonly List<RelativeSyncPath> removals = [];

    internal void Upsert(SyncBaselineEntry entry)
    {
        lock (gate)
            upserts.Add(entry);
    }

    internal void Remove(RelativeSyncPath path)
    {
        lock (gate)
            removals.Add(path);
    }

    internal (IReadOnlyList<SyncBaselineEntry> Upserts, IReadOnlyList<RelativeSyncPath> Removals) Take()
    {
        lock (gate)
        {
            var taken = (Upserts: (IReadOnlyList<SyncBaselineEntry>)[.. upserts], Removals: (IReadOnlyList<RelativeSyncPath>)[.. removals]);
            upserts.Clear();
            removals.Clear();

            return taken;
        }
    }
}
