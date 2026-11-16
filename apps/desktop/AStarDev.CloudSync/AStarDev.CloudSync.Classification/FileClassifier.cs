using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Classifies a path by running every signal over the cached vocabulary.</summary>
public sealed class FileClassifier(IEnumerable<IClassificationSignal> signals, IClassificationVocabularyProvider vocabularyProvider) : IFileClassifier
{
    /// <inheritdoc />
    public Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotImplementedException($"{signals}{vocabularyProvider}{path}{cancellationToken}");
}
