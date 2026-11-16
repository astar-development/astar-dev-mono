namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="TermSortColumn"/> cases.</summary>
public static class TermSortColumnFactory
{
    /// <summary>Creates the <see cref="TermSortColumn.Term"/> case.</summary>
    public static TermSortColumn CreateTerm() => new TermSortColumn.Term();

    /// <summary>Creates the <see cref="TermSortColumn.Category"/> case.</summary>
    public static TermSortColumn CreateCategory() => new TermSortColumn.Category();

    /// <summary>Creates the <see cref="TermSortColumn.Source"/> case.</summary>
    public static TermSortColumn CreateSource() => new TermSortColumn.Source();

    /// <summary>Creates the <see cref="TermSortColumn.FileCount"/> case.</summary>
    public static TermSortColumn CreateFileCount() => new TermSortColumn.FileCount();

    /// <summary>Creates the <see cref="TermSortColumn.Ignored"/> case.</summary>
    public static TermSortColumn CreateIgnored() => new TermSortColumn.Ignored();
}
