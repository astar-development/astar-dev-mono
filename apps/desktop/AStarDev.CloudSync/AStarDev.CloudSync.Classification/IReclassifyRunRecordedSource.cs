namespace AStarDev.CloudSync.Classification;

/// <summary>Announces that a completed reclassify run was recorded, so a listener can re-check whether a rerun is still worth recommending. The event is raised on the thread that finished the run, which is rarely the UI thread; a listener must unsubscribe when it is done.</summary>
public interface IReclassifyRunRecordedSource
{
    /// <summary>Raised after a completed run was written to the run store. Not raised for a cancelled, failed or refused run, or when the write failed.</summary>
    event EventHandler RunRecorded;
}
