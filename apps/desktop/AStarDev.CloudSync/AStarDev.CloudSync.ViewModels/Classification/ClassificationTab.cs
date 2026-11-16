namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The tab shown in the classification view.</summary>
public abstract record ClassificationTab
{
    private ClassificationTab()
    {
    }

    /// <summary>The term list.</summary>
    public sealed record Terms : ClassificationTab;

    /// <summary>The suggestions list.</summary>
    public sealed record Suggestions : ClassificationTab;
}
