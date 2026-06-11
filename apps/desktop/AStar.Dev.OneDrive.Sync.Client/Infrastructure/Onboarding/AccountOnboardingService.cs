using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;

/// <inheritdoc />
public sealed class AccountOnboardingService(IAccountRepository accountRepository, ISyncRuleRepository syncRuleRepository) : IAccountOnboardingService
{
    /// <inheritdoc />
    public async Task<OneDriveAccount> CompleteOnboardingAsync(OneDriveAccount account, CancellationToken ct)
    {
        if (account.SyncConfig is Option<AccountSyncConfig>.None)
            account.SyncConfig = ResolveDefaultSyncConfig(account.Profile.Email);

        await accountRepository.UpsertAsync(ToEntity(account), ct);

        foreach (var (folderId, folderName) in account.FolderNames)
            await syncRuleRepository.UpsertAsync(account.Id, $"/{folderName}", RuleType.Include, folderId.Id, ct);

        if (account.IsActive)
            await accountRepository.SetActiveAccountAsync(account.Id, ct);

        return account;
    }

    private static Option<AccountSyncConfig> ResolveDefaultSyncConfig(string email)
    {
        string defaultPath = ApplicationMetadata.ApplicationNameHyphenated.UserDirectory().CombinePath(email);

        return LocalSyncPathFactory.Create(defaultPath)
            .Match(p => Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, p)), _ => Option.None<AccountSyncConfig>());
    }

    private static AccountEntity ToEntity(OneDriveAccount account)
        => new()
        {
            Id = account.Id,
            Profile = account.Profile,
            AccentIndex = account.AccentIndex,
            IsActive = account.IsActive,
            LastSyncedAt = account.LastSyncedAt,
            Quota = account.Quota,
            SyncConfig = account.SyncConfig.Match(v => v, () => AccountSyncConfigFactory.Default)
        };
}
