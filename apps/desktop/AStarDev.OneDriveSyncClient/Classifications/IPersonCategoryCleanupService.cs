using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Removes Person child categories that were created by filename heuristics that no longer apply.</summary>
public interface IPersonCategoryCleanupService
{
    /// <summary>
    /// Removes each Level 2 category under Person whose linked files no longer produce that name when re-categorised, together with its file links.
    /// Categories without linked files are kept. Returns the number of categories removed.
    /// </summary>
    Task<Result<int, string>> RemoveInvalidPersonCategoriesAsync(CancellationToken cancellationToken = default);
}
