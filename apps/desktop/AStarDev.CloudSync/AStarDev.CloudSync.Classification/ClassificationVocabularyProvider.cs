using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Loads the terms and categories once and keeps the vocabulary until <see cref="Invalidate"/>. Construct it over the undecorated stores; see <see cref="CachingTermStore"/> for the registration order.</summary>
public sealed class ClassificationVocabularyProvider(ITermStore termStore, ICategoryStore categoryStore) : IClassificationVocabularyProvider
{
    /// <inheritdoc />
    public Task<Result<ClassificationVocabulary, ClassificationFailure>> GetAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{termStore}{categoryStore}{cancellationToken}");

    /// <inheritdoc />
    public void Invalidate() => throw new NotImplementedException($"{termStore}{categoryStore}");
}
