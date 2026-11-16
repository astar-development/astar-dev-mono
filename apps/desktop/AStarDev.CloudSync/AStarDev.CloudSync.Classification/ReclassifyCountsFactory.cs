namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyCounts"/> values.</summary>
public static class ReclassifyCountsFactory
{
    /// <summary>Creates the counts.</summary>
    /// <param name="filesWritten">The files written.</param>
    /// <param name="batches">The batches committed.</param>
    /// <param name="duplicatesSkipped">The duplicates skipped.</param>
    public static ReclassifyCounts Create(int filesWritten, int batches, int duplicatesSkipped) => new(filesWritten, batches, duplicatesSkipped);
}
