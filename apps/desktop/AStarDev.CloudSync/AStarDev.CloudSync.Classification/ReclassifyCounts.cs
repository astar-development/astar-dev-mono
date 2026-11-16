namespace AStarDev.CloudSync.Classification;

/// <summary>What a reclassify run got done. Only committed batches count.</summary>
public sealed record ReclassifyCounts
{
    internal ReclassifyCounts(int filesWritten, int batches, int duplicatesSkipped)
    {
        FilesWritten = filesWritten;
        Batches = batches;
        DuplicatesSkipped = duplicatesSkipped;
    }

    /// <summary>The files whose automatic links were replaced.</summary>
    public int FilesWritten { get; }

    /// <summary>The batches committed.</summary>
    public int Batches { get; }

    /// <summary>The streamed files skipped because the same account and path (ignoring case) came earlier.</summary>
    public int DuplicatesSkipped { get; }
}
