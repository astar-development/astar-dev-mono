namespace AStarDev.SingleInstance;

/// <summary>What happened when the app tried to become the running instance.</summary>
public abstract record InstanceClaim
{
    private InstanceClaim()
    {
    }

    /// <summary>This process is the running instance and holds the lock until <see cref="Instance"/> is disposed.</summary>
    public sealed record Primary : InstanceClaim
    {
        internal Primary(PrimaryInstance instance) => Instance = instance;

        /// <summary>The held lock and hand-off listener.</summary>
        public PrimaryInstance Instance { get; }
    }

    /// <summary>Another instance is running and agreed to show its window; this process should exit.</summary>
    public sealed record HandedOff : InstanceClaim
    {
        internal HandedOff()
        {
        }
    }

    /// <summary>Another instance holds the lock but did not answer in time; this process should still exit.</summary>
    public sealed record NotAnswered : InstanceClaim
    {
        internal NotAnswered()
        {
        }
    }

    /// <summary>The lock could not be created at all (for example the data folder cannot be written), so single instance cannot be enforced.</summary>
    public sealed record LockUnavailable : InstanceClaim
    {
        internal LockUnavailable()
        {
        }
    }
}
