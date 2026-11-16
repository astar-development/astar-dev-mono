using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Decorates <see cref="ICategoryStore"/> so every successful write invalidates the cached vocabulary. Register it as described on <see cref="CachingTermStore"/>.</summary>
public sealed class CachingCategoryStore(ICategoryStore inner, IClassificationVocabularyProvider provider) : ICategoryStore
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{provider}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{provider}{category}{cancellationToken}");
}
