using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;

namespace AStarDev.OneDriveSyncClient.Startup;

public sealed class StartupService(IAccountRepository repository, ISyncRuleRepository syncRuleRepository, IAuthService authService) : IStartupService
{
    /// <inheritdoc />
    public Task<Result<List<OneDriveAccount>, string>> RestoreAccountsAsync()
        => Try.RunAsync(() => repository.GetAllAsync(CancellationToken.None))
              .BindAsync(FetchWithCachedIdsAsync)
              .BindAsync(BuildFilteredAccountsAsync)
              .TapAsync(EnsureSingleActiveAccount)
              .ToResultAsync(ex => ex.GetBaseException().Message);

    private Task<Exceptional<(List<AccountEntity> entities, HashSet<string> cachedIds)>> FetchWithCachedIdsAsync(List<AccountEntity> entities)
        => Try.RunAsync(async () => (entities, (await authService.GetCachedAccountIdsAsync().ConfigureAwait(false)).ToHashSet()));

    private Task<Exceptional<List<OneDriveAccount>>> BuildFilteredAccountsAsync((List<AccountEntity> entities, HashSet<string> cachedIds) input)
        => Try.RunAsync(() => BuildAccountsAsync(FilterToCachedEntities(input.entities, input.cachedIds)));

    private static IEnumerable<AccountEntity> FilterToCachedEntities(IEnumerable<AccountEntity> entities, HashSet<string> cachedIds)
        => entities.Where(entity => cachedIds.Contains(entity.Id.Value));

    private async Task<List<OneDriveAccount>> BuildAccountsAsync(IEnumerable<AccountEntity> entities)
    {
        List<OneDriveAccount> accounts = [];

        foreach (var entity in entities)
        {
            var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, CancellationToken.None).ConfigureAwait(false);
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
