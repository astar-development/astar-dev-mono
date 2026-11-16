namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The column the term list is sorted by.</summary>
public abstract record TermSortColumn
{
    private TermSortColumn()
    {
    }

    /// <summary>The term text.</summary>
    public sealed record Term : TermSortColumn;

    /// <summary>The category name.</summary>
    public sealed record Category : TermSortColumn;

    /// <summary>The source text.</summary>
    public sealed record Source : TermSortColumn;

    /// <summary>The number of linked files.</summary>
    public sealed record FileCount : TermSortColumn;

    /// <summary>Whether the term is ignored.</summary>
    public sealed record Ignored : TermSortColumn;
}
