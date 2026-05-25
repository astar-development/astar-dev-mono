using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface IFileClassificationRuleRepository
{
    /// <summary>Returns all classification rules persisted in the database.</summary>
    Task<IReadOnlyList<FileClassificationRule>> GetAllAsync(CancellationToken cancellationToken = default);
}
