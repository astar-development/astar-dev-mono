using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Data.Repositories;

/// <summary>Repository for reading and writing the hierarchical file classification taxonomy.</summary>
public interface IFileClassificationRepository
{
    /// <summary>Returns all category nodes in the hierarchy.</summary>
    Task<IReadOnlyList<FileClassificationCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a simple list of all category nodes in the hierarchy.</summary>
    Task<IReadOnlyList<FileClassificationCategoryEntity>> GetAllCategoriesSimpleAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether the specified file already has any classification rows, from any application.</summary>
    Task<bool> HasClassificationsAsync(FileId fileDetailId, CancellationToken cancellationToken = default);

    /// <summary>Adds one classification row per category for the specified file.</summary>
    Task AddClassificationsAsync(FileId fileDetailId, IReadOnlyList<int> categoryIds, CancellationToken cancellationToken = default);

    /// <summary>Persists a new category and returns its generated identifier.</summary>
    Task<Result<FileClassificationCategoryId, string>> AddCategoryAsync(FileClassificationCategory category, CancellationToken cancellationToken = default);

    /// <summary>Updates the mutable fields of an existing category and returns its identifier.</summary>
    Task<Result<FileClassificationCategoryId, string>> UpdateCategoryAsync(FileClassificationCategoryId id, FileClassificationCategory category, CancellationToken cancellationToken = default);

    /// <summary>Reassigns a category's parent, recalculating its level and cascading the level change to all of its descendants. Fails if the category or the new parent do not exist, or if the new parent is the category itself or one of its own descendants.</summary>
    Task<Result<FileClassificationCategoryId, string>> ReparentCategoryAsync(FileClassificationCategoryId id, Option<FileClassificationCategoryId> newParentId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a category. No-op if the category does not exist.</summary>
    Task DeleteCategoryAsync(FileClassificationCategoryId id, CancellationToken cancellationToken = default);

    /// <summary>Deletes all keywords and all categories in the sequence. Used after importing a new taxonomy.</summary>
    Task DeleteAsync(IEnumerable<FileClassificationCategoryId> fileClassificationCategoryIds, CancellationToken cancellationToken = default);
}
