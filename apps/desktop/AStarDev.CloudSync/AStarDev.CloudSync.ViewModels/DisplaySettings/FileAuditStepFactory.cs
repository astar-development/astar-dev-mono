using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>Creates <see cref="FileAuditStep"/> cases.</summary>
public static class FileAuditStepFactory
{
    /// <summary>Creates the <see cref="FileAuditStep.Idle"/> case.</summary>
    public static FileAuditStep CreateIdle() => new FileAuditStep.Idle();

    /// <summary>Creates the <see cref="FileAuditStep.Confirming"/> case.</summary>
    public static FileAuditStep CreateConfirming() => new FileAuditStep.Confirming();

    /// <summary>Creates the <see cref="FileAuditStep.Running"/> case.</summary>
    public static FileAuditStep CreateRunning() => new FileAuditStep.Running();

    /// <summary>Creates the <see cref="FileAuditStep.Completed"/> case.</summary>
    /// <param name="counts">What the audit found.</param>
    public static FileAuditStep CreateCompleted(FileAuditCounts counts) => new FileAuditStep.Completed(counts);

    /// <summary>Creates the <see cref="FileAuditStep.Failed"/> case.</summary>
    /// <param name="failure">Why the audit could not count.</param>
    public static FileAuditStep CreateFailed(FileAuditFailure failure) => new FileAuditStep.Failed(failure);
}
