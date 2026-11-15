namespace AStarDev.CloudSync.ViewModels.SyncPairs;

/// <summary>Where removing a sync pair (stopping its sync) has got to.</summary>
public abstract record SyncPairRemoveStep
{
    private SyncPairRemoveStep()
    {
    }

    /// <summary>Nothing is being removed.</summary>
    public sealed record Idle : SyncPairRemoveStep
    {
        internal Idle()
        {
        }
    }

    /// <summary>The user is asked to confirm that the folder stops syncing.</summary>
    public sealed record Confirming : SyncPairRemoveStep
    {
        internal Confirming()
        {
        }
    }
}
