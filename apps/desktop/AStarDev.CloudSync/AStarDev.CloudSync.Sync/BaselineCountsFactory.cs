namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="BaselineCounts"/> values.</summary>
public static class BaselineCountsFactory
{
    /// <summary>Creates the counts. Negative values are treated as zero.</summary>
    /// <param name="folders">The folders in the baseline.</param>
    /// <param name="files">The files in the baseline.</param>
    public static BaselineCounts Create(int folders, int files) => new(Math.Max(folders, 0), Math.Max(files, 0));
}
