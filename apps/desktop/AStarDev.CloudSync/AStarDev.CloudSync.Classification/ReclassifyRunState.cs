namespace AStarDev.CloudSync.Classification;

internal sealed class ReclassifyRunState
{
    internal int FilesWritten { get; private set; }

    internal int Batches { get; private set; }

    internal int DuplicatesSkipped { get; private set; }

    internal int SkippedForAccount { get; private set; }

    internal void SkipDuplicate() => DuplicatesSkipped++;

    internal void SkipForAccount(int files) => SkippedForAccount += files;

    internal void CommitBatch(int files)
    {
        FilesWritten += files;
        Batches++;
    }

    internal ReclassifyCounts ToCounts() => ReclassifyCountsFactory.Create(FilesWritten, Batches, DuplicatesSkipped, SkippedForAccount);
}
