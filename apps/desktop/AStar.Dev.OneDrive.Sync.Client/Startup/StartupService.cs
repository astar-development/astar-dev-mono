using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Startup;

public sealed class StartupService(IAccountRepository repository, ISyncRuleRepository syncRuleRepository, IAuthService authService) : IStartupService
{
    public async Task<List<OneDriveAccount>> RestoreAccountsAsync()
    {
        var entities = await repository.GetAllAsync(CancellationToken.None);

        var cachedIds = (await authService.GetCachedAccountIdsAsync()).ToHashSet();

        List<OneDriveAccount> accounts = [];

        foreach(var entity in entities)
        {
            if(!cachedIds.Contains(entity.Id.Id))
                continue;

            var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, CancellationToken.None);

            accounts.Add(new OneDriveAccount
            {
                Id                = entity.Id,
                DisplayName       = entity.DisplayName,
                Email             = entity.Email,
                AccentIndex       = entity.AccentIndex,
                IsActive          = entity.IsActive,
                LastSyncedAt      = entity.LastSyncedAt,
                QuotaTotal        = entity.QuotaTotal,
                QuotaUsed         = entity.QuotaUsed,
                SelectedFolderIds = [.. rules.Where(r => r.RuleType == RuleType.Include && r.RemoteItemId is not null).Select(r => new OneDriveFolderId(r.RemoteItemId!))],
                LocalSyncPath     = entity.LocalSyncPath.Value.Length > 0 ? entity.LocalSyncPath : null,
                ConflictPolicy    = entity.ConflictPolicy
            });
        }

        int activeCount = accounts.Count(a => a.IsActive);
        if(activeCount > 1)
        {
            foreach(var a in accounts.Where(a => a.IsActive).Skip(1))
                a.IsActive = false;
        }

        if(accounts.Count > 0 && !accounts.Any(a => a.IsActive))
            accounts[0].IsActive = true;

        return accounts;
    }
}
