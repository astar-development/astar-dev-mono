using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reads and writes classification categories.</summary>
public interface ICategoryStore
{
    /// <summary>Reads every category.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Adds the category, or returns the stored one when a category with the same name, ignoring case, exists.</summary>
    /// <param name="category">The category to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken);
}
