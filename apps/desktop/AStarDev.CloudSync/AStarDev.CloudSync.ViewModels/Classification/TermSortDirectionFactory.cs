namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="TermSortDirection"/> cases.</summary>
public static class TermSortDirectionFactory
{
    /// <summary>Creates the <see cref="TermSortDirection.Ascending"/> case.</summary>
    public static TermSortDirection CreateAscending() => new TermSortDirection.Ascending();

    /// <summary>Creates the <see cref="TermSortDirection.Descending"/> case.</summary>
    public static TermSortDirection CreateDescending() => new TermSortDirection.Descending();
}
