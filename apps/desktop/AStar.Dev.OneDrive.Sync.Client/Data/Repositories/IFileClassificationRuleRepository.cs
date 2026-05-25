using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface IFileClassificationRuleRepository
{
    /// <summary>Returns all classification rules persisted in the database.</summary>
    Task<IReadOnlyList<FileClassificationRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all classification rules paired with their database Ids.</summary>
    Task<IReadOnlyList<FileClassificationRuleEntry>> GetAllWithIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new rule and returns the assigned database Id.</summary>
    Task<int> AddAsync(FileClassificationRule rule, CancellationToken cancellationToken = default);

    /// <summary>Updates the persisted rule with the specified Id.</summary>
    Task UpdateAsync(int id, FileClassificationRule rule, CancellationToken cancellationToken = default);

    /// <summary>Removes the rule with the given Id. No-op if the Id does not exist.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
