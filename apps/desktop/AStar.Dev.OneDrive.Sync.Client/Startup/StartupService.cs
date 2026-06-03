using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

namespace AStar.Dev.OneDrive.Sync.Client.Startup;

public sealed class StartupService(IAccountRepository repository, ISyncRuleRepository syncRuleRepository, IAuthService authService) : IStartupService
{
    /// <inheritdoc />
    public Task<Result<List<OneDriveAccount>, string>> RestoreAccountsAsync()
        => Try.RunAsync(() => repository.GetAllAsync(CancellationToken.None))
              .BindAsync(FetchWithCachedIdsAsync)
              .BindAsync(BuildFilteredAccountsAsync)
              .TapAsync(EnsureSingleActiveAccount)
              .MapFailureAsync(ex => ex.GetBaseException().Message);

    private Task<Result<(List<AccountEntity> entities, HashSet<string> cachedIds), Exception>> FetchWithCachedIdsAsync(List<AccountEntity> entities)
        => Try.RunAsync(async () => (entities, (await authService.GetCachedAccountIdsAsync()).ToHashSet()));

    private Task<Result<List<OneDriveAccount>, Exception>> BuildFilteredAccountsAsync((List<AccountEntity> entities, HashSet<string> cachedIds) input)
        => Try.RunAsync(() => BuildAccountsAsync(FilterToCachedEntities(input.entities, input.cachedIds)));

    private static IEnumerable<AccountEntity> FilterToCachedEntities(IEnumerable<AccountEntity> entities, HashSet<string> cachedIds)
        => entities.Where(entity => cachedIds.Contains(entity.Id.Id));

    private async Task<List<OneDriveAccount>> BuildAccountsAsync(IEnumerable<AccountEntity> entities)
    {
        List<OneDriveAccount> accounts = [];

        foreach (var entity in entities)
        {
            var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, CancellationToken.None);
            accounts.Add(BuildOneDriveAccount(entity, rules));
        }

        return accounts;
    }

    private static void EnsureSingleActiveAccount(List<OneDriveAccount> accounts)
    {
        foreach (var extra in accounts.Where(a => a.IsActive).Skip(1).ToList())
            extra.IsActive = false;

        if (accounts.Count > 0 && !accounts.Any(a => a.IsActive))
            accounts[0].IsActive = true;
    }

    private static OneDriveAccount BuildOneDriveAccount(AccountEntity entity, List<SyncRuleEntity> rules) => new OneDriveAccount
    {
        Id = entity.Id,
        Profile = entity.Profile,
        AccentIndex = entity.AccentIndex,
        IsActive = entity.IsActive,
        LastSyncedAt = entity.LastSyncedAt,
        Quota = entity.Quota,
        SelectedFolderIds = [.. rules.Where(r => r.RuleType == RuleType.Include).Choose(r => r.RemoteItemId).Select(id => new OneDriveFolderId(id))],
        SyncConfig = entity.SyncConfig.LocalSyncPath.Value.Length > 0 ? Option.Some(entity.SyncConfig) : Option.None<AccountSyncConfig>()
    };
}
