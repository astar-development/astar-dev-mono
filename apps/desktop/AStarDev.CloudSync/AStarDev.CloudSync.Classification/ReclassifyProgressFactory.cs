namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyProgress"/> values.</summary>
public static class ReclassifyProgressFactory
{
    /// <summary>Creates a progress report.</summary>
    /// <param name="filesDone">The files written so far.</param>
    /// <param name="batch">The batches written so far.</param>
    public static ReclassifyProgress Create(int filesDone, int batch) => Create(filesDone, batch, 0);

    /// <summary>Creates a progress report, with the files skipped because their account was busy or removed.</summary>
    /// <param name="filesDone">The files written so far.</param>
    /// <param name="batch">The batches written so far.</param>
    /// <param name="filesSkipped">The files skipped so far because their account was busy or removed.</param>
    public static ReclassifyProgress Create(int filesDone, int batch, int filesSkipped) => new(filesDone, batch, filesSkipped);
}
