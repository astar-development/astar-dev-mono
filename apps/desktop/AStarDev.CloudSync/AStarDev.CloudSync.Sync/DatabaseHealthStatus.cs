namespace AStarDev.CloudSync.Sync;

/// <summary>What the database health check found this app session (#1612).</summary>
public abstract record DatabaseHealthStatus
{
    private DatabaseHealthStatus()
    {
    }

    /// <summary>The check has not finished yet.</summary>
    public sealed record NotChecked : DatabaseHealthStatus
    {
        internal NotChecked()
        {
        }
    }

    /// <summary>The database passed the check.</summary>
    public sealed record Healthy : DatabaseHealthStatus
    {
        internal Healthy()
        {
        }
    }

    /// <summary>The database failed the check, so nothing syncs this session.</summary>
    public sealed record Failed : DatabaseHealthStatus
    {
        internal Failed(DatabaseHealthFailure failure)
        {
            Failure = failure;
        }

        /// <summary>Why the check failed.</summary>
        public DatabaseHealthFailure Failure { get; }
    }
}
