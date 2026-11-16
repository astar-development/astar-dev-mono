namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The direction of the term list sort.</summary>
public abstract record TermSortDirection
{
    private TermSortDirection()
    {
    }

    /// <summary>Smallest or first first.</summary>
    public sealed record Ascending : TermSortDirection;

    /// <summary>Largest or last first.</summary>
    public sealed record Descending : TermSortDirection;
}
