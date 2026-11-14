namespace AStarDev.CloudSync.Sync;

/// <summary>Takes the progress of one file transfer and adds what is new to its <see cref="PhaseTally"/>.</summary>
/// <param name="tally">The phase the file belongs to.</param>
/// <param name="size">The size of the file.</param>
internal sealed class FileProgress(PhaseTally tally, long size) : IProgress<TransferProgress>
{
    private long seen;

    /// <inheritdoc />
    public void Report(TransferProgress value) => tally.Add(value.BytesDone - Interlocked.Exchange(ref seen, value.BytesDone));

    internal void Finish() => tally.Add(Math.Max(size - Interlocked.Exchange(ref seen, size), 0));
}
