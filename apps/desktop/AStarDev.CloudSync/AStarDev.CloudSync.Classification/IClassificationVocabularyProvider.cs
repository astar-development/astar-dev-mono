using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Serves the classification vocabulary, loaded once and reused until a write invalidates it.</summary>
public interface IClassificationVocabularyProvider
{
    /// <summary>Returns the cached vocabulary, loading it first when none is cached. Concurrent callers share one load. A failed load is not cached.</summary>
    /// <param name="cancellationToken">Cancels the wait and, for the caller that started it, the load.</param>
    Task<Result<ClassificationVocabulary, ClassificationFailure>> GetAsync(CancellationToken cancellationToken);

    /// <summary>Drops the cached vocabulary so the next <see cref="GetAsync"/> reloads it. A load already running never becomes the cached one.</summary>
    void Invalidate();
}
