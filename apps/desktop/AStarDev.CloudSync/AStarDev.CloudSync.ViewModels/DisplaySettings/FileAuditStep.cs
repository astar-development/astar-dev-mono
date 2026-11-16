using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>Where the file audit setting is (#1801).</summary>
public abstract record FileAuditStep
{
    private FileAuditStep()
    {
    }

    /// <summary>Nothing is running; the Audit button shows.</summary>
    public sealed record Idle : FileAuditStep;

    /// <summary>The user pressed Audit and must accept the warning that it can be slow.</summary>
    public sealed record Confirming : FileAuditStep;

    /// <summary>The audit is running and can be stopped.</summary>
    public sealed record Running : FileAuditStep;

    /// <summary>The audit finished.</summary>
    public sealed record Completed : FileAuditStep
    {
        internal Completed(FileAuditCounts counts)
        {
            Counts = counts;
        }

        /// <summary>What the audit found.</summary>
        public FileAuditCounts Counts { get; }
    }

    /// <summary>The audit could not count.</summary>
    public sealed record Failed : FileAuditStep
    {
        internal Failed(FileAuditFailure failure)
        {
            Failure = failure;
        }

        /// <summary>Why.</summary>
        public FileAuditFailure Failure { get; }
    }
}
