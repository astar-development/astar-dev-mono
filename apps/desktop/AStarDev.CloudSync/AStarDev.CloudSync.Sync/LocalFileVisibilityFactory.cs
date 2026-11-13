namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalFileVisibility"/> cases.</summary>
public static class LocalFileVisibilityFactory
{
    /// <summary>Creates the <see cref="LocalFileVisibility.Visible"/> case.</summary>
    public static LocalFileVisibility CreateVisible() => new LocalFileVisibility.Visible();

    /// <summary>Creates the <see cref="LocalFileVisibility.Skipped"/> case.</summary>
    /// <param name="reason">Why the file is left out.</param>
    public static LocalFileVisibility CreateSkipped(FileSkipReason reason) => new LocalFileVisibility.Skipped(reason);
}
