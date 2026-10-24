using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Inserts the default classification taxonomy without duplicating or altering existing categories.</summary>
public interface IDefaultCategorySeedingService
{
    /// <summary>Adds each missing default Level 1 and Level 2 category. Existing categories are matched by name, level and parent (ignoring case) and left untouched. Returns the number of categories added.</summary>
    Task<Result<int, string>> SeedDefaultCategoriesAsync(CancellationToken cancellationToken = default);
}
