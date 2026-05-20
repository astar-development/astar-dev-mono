using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Onboarding;

public sealed class GivenAnAccountOnboardingService
{
    private const string AccountIdStr = "account-1";
    private const string DisplayName  = "Test User";
    private const string Email        = "test@outlook.com";
    private const string FolderId1    = "f1";
    private const string FolderName1  = "Documents";
    private const string FolderId2    = "f2";
    private const string FolderName2  = "Desktop";

    private readonly IAccountRepository  _accountRepository  = Substitute.For<IAccountRepository>();
    private readonly ISyncRuleRepository _syncRuleRepository = Substitute.For<ISyncRuleRepository>();

    private AccountOnboardingService BuildSut() => new(_accountRepository, _syncRuleRepository);

    private static OneDriveAccount BuildAccountWithFolders()
        => new()
        {
            Id        = new AccountId(AccountIdStr),
            Profile   = AccountProfileFactory.Create(DisplayName, Email),
            IsActive  = true,
            FolderNames = new Dictionary<OneDriveFolderId, string>
            {
                { new OneDriveFolderId(FolderId1), FolderName1 },
                { new OneDriveFolderId(FolderId2), FolderName2 }
            }
        };

    private static OneDriveAccount BuildAccountWithNoFolders()
        => new()
        {
            Id        = new AccountId(AccountIdStr),
            Profile   = AccountProfileFactory.Create(DisplayName, Email),
            IsActive  = false,
            FolderNames = []
        };

    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var sut = BuildSut();

        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_completing_onboarding_then_account_is_upserted_to_repository()
    {
        var account = BuildAccountWithFolders();

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        await _accountRepository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_completing_onboarding_with_folders_then_sync_rules_are_written_for_each_folder()
    {
        var account = BuildAccountWithFolders();

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        await _syncRuleRepository.Received().UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName1}", RuleType.Include, FolderId1, Arg.Any<CancellationToken>());
        await _syncRuleRepository.Received().UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName2}", RuleType.Include, FolderId2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_completing_onboarding_with_two_folders_then_exactly_two_sync_rules_are_written()
    {
        var account = BuildAccountWithFolders();

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        await _syncRuleRepository.Received(2).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_completing_onboarding_with_no_folders_then_no_sync_rules_are_written()
    {
        var account = BuildAccountWithNoFolders();

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        await _syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_is_active_then_set_active_account_is_called()
    {
        var account = BuildAccountWithFolders();
        account.IsActive = true;

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        await _accountRepository.Received(1).SetActiveAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_is_not_active_then_set_active_account_is_not_called()
    {
        var account = BuildAccountWithFolders();
        account.IsActive = false;

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        await _accountRepository.DidNotReceive().SetActiveAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_has_no_sync_config_then_sync_config_is_resolved_from_email()
    {
        var account = BuildAccountWithFolders();
        account.SyncConfig = Option.None<AccountSyncConfig>();

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        (account.SyncConfig is Option<AccountSyncConfig>.Some).ShouldBeTrue();
    }

    [Fact]
    public async Task when_account_has_existing_sync_config_then_sync_config_is_not_overwritten()
    {
        var account      = BuildAccountWithFolders();
        var existingPath = LocalSyncPathFactory.Create("/custom/path").Match<LocalSyncPath>(p => p, _ => throw new InvalidOperationException());
        var existingConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, existingPath);
        account.SyncConfig = existingConfig;

        await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        account.SyncConfig.TryGetValue(out var cfg).ShouldBeTrue();
        cfg.ShouldBeSameAs(existingConfig);
    }

    [Fact]
    public async Task when_completing_onboarding_then_same_account_instance_is_returned()
    {
        var account = BuildAccountWithFolders();

        var result = await BuildSut().CompleteOnboardingAsync(account, CancellationToken.None);

        result.ShouldBeSameAs(account);
    }
}
