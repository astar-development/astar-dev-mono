namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyCounts"/> values.</summary>
public static class ReclassifyCountsFactory
{
    /// <summary>Creates the counts.</summary>
    /// <param name="filesWritten">The files written.</param>
    /// <param name="batches">The batches committed.</param>
    /// <param name="duplicatesSkipped">The duplicates skipped.</param>
    public static ReclassifyCounts Create(int filesWritten, int batches, int duplicatesSkipped) => Create(filesWritten, batches, duplicatesSkipped, 0);

    /// <summary>Creates the counts, with the files skipped because their account was busy or removed.</summary>
    /// <param name="filesWritten">The files written.</param>
    /// <param name="batches">The batches committed.</param>
    /// <param name="duplicatesSkipped">The duplicates skipped.</param>
    /// <param name="skippedForAccount">The files skipped because their account was busy or removed.</param>
    public static ReclassifyCounts Create(int filesWritten, int batches, int duplicatesSkipped, int skippedForAccount) => new(filesWritten, batches, duplicatesSkipped, skippedForAccount);
}
