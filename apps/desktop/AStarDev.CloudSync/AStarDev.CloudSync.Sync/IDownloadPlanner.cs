namespace AStarDev.CloudSync.Sync;

/// <summary>Works out what the download pass does, from the remote index, the baseline and the local scan, without touching the file system (#1400).</summary>
public interface IDownloadPlanner
{
    /// <summary>Plans the pass.</summary>
    /// <param name="request">The remote index, baseline, local scan, rules and quarantine.</param>
    /// <returns>The steps in execution order and the deletions the guard held.</returns>
    DownloadPlan Plan(DownloadPlanRequest request);
}
