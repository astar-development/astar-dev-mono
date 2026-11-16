namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyProgress"/> values.</summary>
public static class ReclassifyProgressFactory
{
    /// <summary>Creates a progress report.</summary>
    /// <param name="filesDone">The files written so far.</param>
    /// <param name="batch">The batches written so far.</param>
    public static ReclassifyProgress Create(int filesDone, int batch) => new(filesDone, batch);
}
