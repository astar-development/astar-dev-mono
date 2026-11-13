namespace AStarDev.CloudSync.Sync;

/// <summary>Turns the local change set into upload steps by comparing it with the baseline and the remote index. Pure: it reads nothing and changes nothing.</summary>
public interface IUploadPlanner
{
    /// <summary>Plans the upload pass.</summary>
    /// <param name="changes">What changed on disk (#1399). Deferred and unreadable paths produce no step.</param>
    /// <param name="baseline">The baseline rows by path.</param>
    /// <param name="remote">The remote index items by path, including everything below any folder a change touches.</param>
    /// <param name="rules">The account's selection rules. Paths outside an include rule, and download-only pairs, produce no step.</param>
    /// <param name="names">The provider's name rules.</param>
    /// <returns>The ordered plan. It has one step per path ignoring case, because the change set already holds one change per path.</returns>
    UploadPlan Plan(LocalChangeSet changes, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, IReadOnlyList<SelectionRule> rules, IRemoteNamePolicy names);
}
