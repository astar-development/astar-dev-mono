using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

/// <summary>Repository for reading and writing the hierarchical file classification taxonomy.</summary>
public interface IFileClassificationRepository
{
    /// <summary>Returns all category nodes in the hierarchy.</summary>
    Task<IReadOnlyList<FileClassificationCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all keywords belonging to the specified category.</summary>
    Task<IReadOnlyList<FileClassificationKeyword>> GetKeywordsForCategoryAsync(FileClassificationCategoryId categoryId, CancellationToken cancellationToken = default);

    /// <summary>Returns all keywords projected as flat <see cref="KeywordMapping"/> records for use in classification pipelines.</summary>
    Task<IReadOnlyList<KeywordMapping>> GetAllKeywordMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new category and returns its generated identifier.</summary>
    Task<Result<FileClassificationCategoryId, string>> AddCategoryAsync(FileClassificationCategory category, CancellationToken cancellationToken = default);

    /// <summary>Updates the mutable fields of an existing category and returns its identifier.</summary>
    Task<Result<FileClassificationCategoryId, string>> UpdateCategoryAsync(FileClassificationCategoryId id, FileClassificationCategory category, CancellationToken cancellationToken = default);

    /// <summary>Deletes a category. No-op if the category does not exist.</summary>
    Task DeleteCategoryAsync(FileClassificationCategoryId id, CancellationToken cancellationToken = default);

    /// <summary>Adds a keyword to a leaf category and returns the generated keyword identifier.</summary>
    Task<Result<int, string>> AddKeywordAsync(FileClassificationCategoryId categoryId, FileClassificationKeyword keyword, CancellationToken cancellationToken = default);

    /// <summary>Updates a keyword's value. Fails if the owning category is not a leaf.</summary>
    Task<Result<int, string>> UpdateKeywordAsync(int keywordId, FileClassificationKeyword keyword, CancellationToken cancellationToken = default);

    /// <summary>Deletes a keyword. No-op if the keyword does not exist.</summary>
    Task DeleteKeywordAsync(int keywordId, CancellationToken cancellationToken = default);
}
