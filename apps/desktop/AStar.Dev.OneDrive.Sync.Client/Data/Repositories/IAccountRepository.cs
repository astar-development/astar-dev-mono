using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface IAccountRepository
{
    Task<List<AccountEntity>> GetAllAsync();
    Task<AccountEntity?> GetByIdAsync(string id);
    Task UpsertAsync(AccountEntity account);
    Task DeleteAsync(string id);
    Task SetActiveAccountAsync(string id);
    Task UpdateDeltaLinkAsync(string accountId, string folderId, string deltaLink);
}
