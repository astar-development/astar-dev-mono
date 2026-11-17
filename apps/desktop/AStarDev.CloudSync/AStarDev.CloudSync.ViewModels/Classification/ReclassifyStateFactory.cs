using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="ReclassifyState"/> cases.</summary>
public static class ReclassifyStateFactory
{
    /// <summary>Creates the <see cref="ReclassifyState.Idle"/> case.</summary>
    public static ReclassifyState CreateIdle() => new ReclassifyState.Idle();

    /// <summary>Creates the <see cref="ReclassifyState.Running"/> case. A negative count becomes zero.</summary>
    /// <param name="filesDone">The files written so far.</param>
    public static ReclassifyState CreateRunning(int filesDone) => CreateRunning(filesDone, 0);

    /// <summary>Creates the <see cref="ReclassifyState.Running"/> case, with the files skipped because their account was busy or removed. A negative count becomes zero.</summary>
    /// <param name="filesDone">The files written so far.</param>
    /// <param name="filesSkipped">The files skipped so far.</param>
    public static ReclassifyState CreateRunning(int filesDone, int filesSkipped) => new ReclassifyState.Running(Math.Max(filesDone, 0), Math.Max(filesSkipped, 0));

    /// <summary>Creates the <see cref="ReclassifyState.Observing"/> case, for a run this page did not start. A negative count becomes zero.</summary>
    /// <param name="filesDone">The files written so far.</param>
    /// <param name="filesSkipped">The files skipped so far.</param>
    public static ReclassifyState CreateObserving(int filesDone, int filesSkipped) => new ReclassifyState.Observing(Math.Max(filesDone, 0), Math.Max(filesSkipped, 0));

    /// <summary>Creates the <see cref="ReclassifyState.Completed"/> case.</summary>
    /// <param name="counts">What the run did.</param>
    public static ReclassifyState CreateCompleted(ReclassifyCounts counts) => new ReclassifyState.Completed(counts);

    /// <summary>Creates the <see cref="ReclassifyState.Cancelled"/> case.</summary>
    /// <param name="counts">What the run did before the cancel.</param>
    public static ReclassifyState CreateCancelled(ReclassifyCounts counts) => new ReclassifyState.Cancelled(counts);

    /// <summary>Creates the <see cref="ReclassifyState.Failed"/> case.</summary>
    /// <param name="counts">What the run did before the failure.</param>
    public static ReclassifyState CreateFailed(ReclassifyCounts counts) => new ReclassifyState.Failed(counts);

    /// <summary>Creates the <see cref="ReclassifyState.AlreadyRunning"/> case.</summary>
    public static ReclassifyState CreateAlreadyRunning() => new ReclassifyState.AlreadyRunning();
}
