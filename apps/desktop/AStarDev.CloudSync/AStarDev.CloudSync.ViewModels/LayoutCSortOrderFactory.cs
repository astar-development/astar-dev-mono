namespace AStarDev.CloudSync.ViewModels;

/// <summary>Creates <see cref="LayoutCSortOrder"/> cases.</summary>
public static class LayoutCSortOrderFactory
{
    /// <summary>Creates the needs-attention-first order.</summary>
    public static LayoutCSortOrder CreateNeedsAttentionFirst() => new LayoutCSortOrder.NeedsAttentionFirst();

    /// <summary>Creates the by-name order.</summary>
    public static LayoutCSortOrder CreateByName() => new LayoutCSortOrder.ByName();
}
