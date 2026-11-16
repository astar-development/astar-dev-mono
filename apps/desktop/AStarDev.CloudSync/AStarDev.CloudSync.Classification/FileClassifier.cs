using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Classifies a path by running every signal over the cached vocabulary. A vocabulary that cannot be loaded classifies nothing and is retried on the next file.</summary>
public sealed class FileClassifier(IEnumerable<IClassificationSignal> signals, IClassificationVocabularyProvider vocabularyProvider) : IFileClassifier
{
    /// <inheritdoc />
    public Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken) =>
        vocabularyProvider.GetAsync(cancellationToken).MatchAsync(
            vocabulary => ClassificationCombiner.Combine(path, signals.SelectMany(signal => signal.Evaluate(path, vocabulary))),
            _ => ClassificationCombiner.Combine(path, []));
}
