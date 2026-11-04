namespace AStarDev.CloudSync.ViewModels;

/// <summary>The order of layout C's sync pair grid.</summary>
public abstract record LayoutCSortOrder
{
    private LayoutCSortOrder()
    {
    }

    /// <summary>Conflicts first, then syncing pairs, then the rest in their original order.</summary>
    public sealed record NeedsAttentionFirst : LayoutCSortOrder;

    /// <summary>Alphabetical by pair name.</summary>
    public sealed record ByName : LayoutCSortOrder;
}
