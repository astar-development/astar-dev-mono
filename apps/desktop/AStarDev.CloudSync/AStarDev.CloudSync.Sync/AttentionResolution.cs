namespace AStarDev.CloudSync.Sync;

/// <summary>How the user settled an attention item. Closing an item only records the choice; applying it belongs to the resolution UI (#1406).</summary>
public abstract record AttentionResolution
{
    private AttentionResolution()
    {
    }

    /// <summary>Keep the local file.</summary>
    public sealed record KeepLocal : AttentionResolution
    {
        internal KeepLocal()
        {
        }
    }

    /// <summary>Keep the cloud file.</summary>
    public sealed record KeepCloud : AttentionResolution
    {
        internal KeepCloud()
        {
        }
    }

    /// <summary>Keep both files.</summary>
    public sealed record KeepBoth : AttentionResolution
    {
        internal KeepBoth()
        {
        }
    }

    /// <summary>Let the held deletions go ahead.</summary>
    public sealed record ConfirmDeletions : AttentionResolution
    {
        internal ConfirmDeletions()
        {
        }
    }

    /// <summary>The engine closed the conflict itself because one side returned to the baseline (#1534).</summary>
    public sealed record AutoCleared : AttentionResolution
    {
        internal AutoCleared()
        {
        }
    }

    /// <summary>Restore the items the held deletions would remove.</summary>
    public sealed record RestoreItems : AttentionResolution
    {
        internal RestoreItems()
        {
        }
    }
}
