namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPlanRequest"/> values.</summary>
public static class DownloadPlanRequestFactory
{
    /// <summary>Creates a request. Missing collections read as empty.</summary>
    /// <param name="remote">Every live remote index item with its path.</param>
    /// <param name="baseline">The sync baseline.</param>
    /// <param name="local">The local scan result.</param>
    /// <param name="rules">The selection rules.</param>
    /// <param name="quarantine">The quarantined paths.</param>
    /// <param name="missingLocally">Baseline files missing on disk without a reported deletion.</param>
    /// <returns>The request.</returns>
    public static DownloadPlanRequest Create(IEnumerable<RemoteIndexEntry> remote, IEnumerable<SyncBaselineEntry> baseline, LocalChangeSet local, IEnumerable<SelectionRule> rules, QuarantineSnapshot quarantine, IEnumerable<RelativeSyncPath> missingLocally) =>
        new([.. remote ?? []], [.. baseline ?? []], local ?? LocalChangeSetFactory.Create([], 0, 0), [.. rules ?? []], quarantine ?? QuarantineSnapshotFactory.CreateEmpty(), [.. missingLocally ?? []]);
}
