using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Classifies a path by running every signal over the stored vocabulary.</summary>
public sealed class FileClassifier(IEnumerable<IClassificationSignal> signals, ITermStore termStore, ICategoryStore categoryStore) : IFileClassifier
{
    /// <inheritdoc />
    public Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotImplementedException($"{path}{signals}{termStore}{categoryStore}{cancellationToken}");
}
