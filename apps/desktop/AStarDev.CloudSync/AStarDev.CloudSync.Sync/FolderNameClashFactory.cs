namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderNameClash"/> cases.</summary>
public static class FolderNameClashFactory
{
    /// <summary>Creates the <see cref="FolderNameClash.Unique"/> case.</summary>
    public static FolderNameClash CreateUnique() => new FolderNameClash.Unique();

    /// <summary>Creates the <see cref="FolderNameClash.CaseClash"/> case.</summary>
    public static FolderNameClash CreateCaseClash() => new FolderNameClash.CaseClash();
}
