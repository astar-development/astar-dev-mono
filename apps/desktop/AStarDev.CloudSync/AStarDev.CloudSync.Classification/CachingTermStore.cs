using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>
/// Decorates <see cref="ITermStore"/> so every successful write invalidates the cached vocabulary. Register the concrete store, then
/// <see cref="ClassificationVocabularyProvider"/> over the concrete stores, then this decorator as <see cref="ITermStore"/> and
/// <see cref="CachingCategoryStore"/> as <see cref="ICategoryStore"/>. The provider must read the concrete stores, never the decorators.
/// </summary>
public sealed class CachingTermStore(ITermStore inner, IClassificationVocabularyProvider provider) : ITermStore
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{provider}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{provider}{term}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{provider}{termId}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{provider}{termId}{status}{cancellationToken}");
}
