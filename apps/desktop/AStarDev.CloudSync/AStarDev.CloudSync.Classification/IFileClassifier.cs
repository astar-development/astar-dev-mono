using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Classifies one file path against the current vocabulary.</summary>
public interface IFileClassifier
{
    /// <summary>Finds the candidates for <paramref name="path"/>. Never throws: a failure gives a result with no candidates.</summary>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken);
}
