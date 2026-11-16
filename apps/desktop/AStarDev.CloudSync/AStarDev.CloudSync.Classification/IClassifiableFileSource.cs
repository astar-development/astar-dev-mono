namespace AStarDev.CloudSync.Classification;

/// <summary>Streams every file the baseline knows about, for a reclassification run.</summary>
public interface IClassifiableFileSource
{
    /// <summary>Streams the files in batches, so the whole baseline is never held in memory.</summary>
    /// <param name="cancellationToken">Cancels the stream.</param>
    IAsyncEnumerable<ClassifiableFile> StreamAsync(CancellationToken cancellationToken);
}
