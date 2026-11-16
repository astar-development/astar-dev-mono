using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Classifies a path by running every signal over the stored vocabulary.</summary>
public sealed class FileClassifier(IEnumerable<IClassificationSignal> signals, ITermStore termStore, ICategoryStore categoryStore) : IFileClassifier
{
    /// <inheritdoc />
    public async Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken)
    {
        var vocabulary = await LoadVocabularyAsync(cancellationToken).ConfigureAwait(false);

        return ClassificationCombiner.Combine(path, signals.SelectMany(signal => signal.Evaluate(path, vocabulary)));
    }

    private Task<ClassificationVocabulary> LoadVocabularyAsync(CancellationToken cancellationToken) =>
        termStore.GetAllAsync(cancellationToken).MatchAsync(
            terms => categoryStore.GetAllAsync(cancellationToken).MatchAsync(categories => Task.FromResult(ClassificationVocabularyFactory.Create(categories, terms)), _ => Task.FromResult(ClassificationVocabularyFactory.Create([], []))),
            _ => Task.FromResult(ClassificationVocabularyFactory.Create([], [])));
}
