namespace AStarDev.CloudSync.Sync;

/// <summary>Why <see cref="IHeldDeletionDecider"/> did not settle a held set (#1643). The set stays open in every case.</summary>
public abstract record HeldDeletionRefusal
{
    private HeldDeletionRefusal()
    {
    }

    /// <summary>A sync pass or another resolution is running for the account.</summary>
    public sealed record Busy : HeldDeletionRefusal
    {
        internal Busy()
        {
        }
    }

    /// <summary>Sync is paused for an update or an account removal.</summary>
    public sealed record Draining : HeldDeletionRefusal
    {
        internal Draining()
        {
        }
    }

    /// <summary>The open held sets could not be read, so nothing was attempted.</summary>
    public sealed record QueueUnreadable : HeldDeletionRefusal
    {
        internal QueueUnreadable()
        {
        }
    }

    /// <summary>The held set is no longer open.</summary>
    public sealed record NoLongerHeld : HeldDeletionRefusal
    {
        internal NoLongerHeld()
        {
        }
    }

    /// <summary>The deletions or restores were attempted but not all were done, so the set stays open.</summary>
    public sealed record NotSettled : HeldDeletionRefusal
    {
        internal NotSettled()
        {
        }
    }
}
