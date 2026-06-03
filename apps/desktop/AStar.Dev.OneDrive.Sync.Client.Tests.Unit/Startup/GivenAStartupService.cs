using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Startup;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Startup;

public sealed class GivenAStartupService
{
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncRuleRepository syncRuleRepository = Substitute.For<ISyncRuleRepository>();
    private readonly IAuthService authService = Substitute.For<IAuthService>();

    private StartupService CreateSut() => new(accountRepository, syncRuleRepository, authService);

    private static AccountEntity BuildEntity(string id, bool isActive = false) => new()
    {
        Id = new AccountId(id),
        Profile = AccountProfileFactory.Empty,
        IsActive = isActive,
        SyncConfig = AccountSyncConfigFactory.Default
    };

    [Fact]
    public async Task when_repository_returns_no_entities_then_empty_list_returned()
    {
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        authService.GetCachedAccountIdsAsync().Returns(["any-id"]);

        var result = await CreateSut().RestoreAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_auth_service_has_no_cached_ids_then_empty_list_returned()
    {
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([BuildEntity("user-1")]);
        authService.GetCachedAccountIdsAsync().Returns([]);

        var result = await CreateSut().RestoreAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_entity_id_not_in_cache_then_entity_is_excluded()
    {
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([BuildEntity("not-cached")]);
        authService.GetCachedAccountIdsAsync().Returns(["different-id"]);

        var result = await CreateSut().RestoreAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_entity_id_is_in_cache_then_entity_is_included()
    {
        var entity = BuildEntity("user-1");
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([entity]);
        authService.GetCachedAccountIdsAsync().Returns(["user-1"]);
        syncRuleRepository.GetByAccountIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().RestoreAccountsAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(entity.Id);
    }

    [Fact]
    public async Task when_multiple_accounts_are_active_then_only_first_remains_active()
    {
        var entity1 = BuildEntity("user-1", isActive: true);
        var entity2 = BuildEntity("user-2", isActive: true);
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([entity1, entity2]);
        authService.GetCachedAccountIdsAsync().Returns(["user-1", "user-2"]);
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().RestoreAccountsAsync();

        result.Count(a => a.IsActive).ShouldBe(1);
        result[0].IsActive.ShouldBeTrue();
        result[1].IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task when_no_account_is_active_but_list_is_not_empty_then_first_is_set_active()
    {
        var entity1 = BuildEntity("user-1", isActive: false);
        var entity2 = BuildEntity("user-2", isActive: false);
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([entity1, entity2]);
        authService.GetCachedAccountIdsAsync().Returns(["user-1", "user-2"]);
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().RestoreAccountsAsync();

        result[0].IsActive.ShouldBeTrue();
        result[1].IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task when_exactly_one_account_is_active_then_active_state_unchanged()
    {
        var entity1 = BuildEntity("user-1", isActive: true);
        var entity2 = BuildEntity("user-2", isActive: false);
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([entity1, entity2]);
        authService.GetCachedAccountIdsAsync().Returns(["user-1", "user-2"]);
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().RestoreAccountsAsync();

        result[0].IsActive.ShouldBeTrue();
        result[1].IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task when_include_rules_exist_then_folder_ids_are_populated()
    {
        var entity = BuildEntity("user-1");
        var rule = new SyncRuleEntity
        {
            AccountId = entity.Id,
            RuleType = RuleType.Include,
            RemoteItemId = Option.Some("folder-abc")
        };
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([entity]);
        authService.GetCachedAccountIdsAsync().Returns(["user-1"]);
        syncRuleRepository.GetByAccountIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns([rule]);

        var result = await CreateSut().RestoreAccountsAsync();

        result[0].SelectedFolderIds.Count.ShouldBe(1);
        result[0].SelectedFolderIds[0].Id.ShouldBe("folder-abc");
    }

    [Fact]
    public async Task when_exclude_rules_exist_then_folder_ids_are_not_populated()
    {
        var entity = BuildEntity("user-1");
        var rule = new SyncRuleEntity
        {
            AccountId = entity.Id,
            RuleType = RuleType.Exclude,
            RemoteItemId = Option.Some("folder-xyz")
        };
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([entity]);
        authService.GetCachedAccountIdsAsync().Returns(["user-1"]);
        syncRuleRepository.GetByAccountIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns([rule]);

        var result = await CreateSut().RestoreAccountsAsync();

        result[0].SelectedFolderIds.ShouldBeEmpty();
    }
}
