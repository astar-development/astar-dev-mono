namespace AStarDev.CloudSync.Classification;

/// <summary>Progress of a reclassify run. The total is not reported because the baseline is streamed, not counted first.</summary>
public sealed record ReclassifyProgress
{
    internal ReclassifyProgress(int filesDone, int batch)
    {
        FilesDone = filesDone;
        Batch = batch;
    }

    /// <summary>The files whose links are written so far.</summary>
    public int FilesDone { get; }

    /// <summary>The number of batches written so far.</summary>
    public int Batch { get; }
}
