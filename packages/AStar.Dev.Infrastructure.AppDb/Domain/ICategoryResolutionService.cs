namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Resolves <see cref="FileClassification"/> text levels to leaf category IDs, inserting missing nodes as it walks the hierarchy.</summary>
public interface ICategoryResolutionService
{
    /// <summary>Resolves each classification to its leaf category ID, inserting missing nodes. Returns distinct leaf IDs.</summary>
    Task<IReadOnlyList<int>> ResolveManyAsync(IReadOnlyList<FileClassification> classifications, CancellationToken cancellationToken);
}
