using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public sealed class StartupService(
    IAccountRepository repository,
    IAuthService authService) : IStartupService
{
    public async Task<List<OneDriveAccount>> RestoreAccountsAsync()
    {
        var entities = await repository.GetAllAsync(CancellationToken.None);

        var cachedIds = (await authService.GetCachedAccountIdsAsync()).ToHashSet();

        List<OneDriveAccount> accounts = [];

        foreach (var entity in entities)
        {
            if (!cachedIds.Contains(entity.Id))
                continue;

            accounts.Add(new OneDriveAccount
            {
                Id = entity.Id,
                DisplayName = entity.DisplayName,
                Email = entity.Email,
                AccentIndex = entity.AccentIndex,
                IsActive = entity.IsActive,
                DeltaLink = entity.DeltaLink,
                LastSyncedAt = entity.LastSyncedAt,
                QuotaTotal = entity.QuotaTotal,
                QuotaUsed = entity.QuotaUsed,
                SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)],
                LocalSyncPath = entity.LocalSyncPath,
                ConflictPolicy = entity.ConflictPolicy
            });
        }

        int activeCount = accounts.Count(a => a.IsActive);
        if (activeCount > 1)
        {
            foreach (var a in accounts.Where(a => a.IsActive).Skip(1))
                a.IsActive = false;
        }

        if (accounts.Count > 0 && !accounts.Any(a => a.IsActive))
            accounts[0].IsActive = true;

        return accounts;
    }
}
