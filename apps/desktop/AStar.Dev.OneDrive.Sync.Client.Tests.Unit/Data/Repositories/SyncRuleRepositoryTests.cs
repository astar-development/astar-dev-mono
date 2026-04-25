using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenASyncRuleRepository : IDisposable
{
    private const string AccountIdString = "user-1";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext     _seedingContext;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public GivenASyncRuleRepository()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _seedingContext = new AppDbContext(options);
        _seedingContext.Database.EnsureCreated();

        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(new AppDbContext(options)));
    }

    public void Dispose()
    {
        _seedingContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task when_upsert_is_called_with_a_new_rule_then_the_rule_is_persisted()
    {
        await SeedAccountAsync();
        var repository = new SyncRuleRepository(_factory);

        await repository.UpsertAsync(new AccountId(AccountIdString), "/Photos", RuleType.Include, TestContext.Current.CancellationToken);

        var rules = await _seedingContext.SyncRules.AsNoTracking().Where(r => r.AccountId == new AccountId(AccountIdString)).ToListAsync(TestContext.Current.CancellationToken);
        rules.Count.ShouldBe(1);
        rules[0].RemotePath.ShouldBe("/Photos");
        rules[0].RuleType.ShouldBe(RuleType.Include);
    }

    [Fact]
    public async Task when_upsert_is_called_twice_for_same_path_then_only_one_rule_exists()
    {
        await SeedAccountAsync();
        var repository = new SyncRuleRepository(_factory);

        await repository.UpsertAsync(new AccountId(AccountIdString), "/Photos", RuleType.Include, TestContext.Current.CancellationToken);
        await repository.UpsertAsync(new AccountId(AccountIdString), "/Photos", RuleType.Exclude, TestContext.Current.CancellationToken);

        var rules = await _seedingContext.SyncRules.AsNoTracking().Where(r => r.AccountId == new AccountId(AccountIdString)).ToListAsync(TestContext.Current.CancellationToken);
        rules.Count.ShouldBe(1);
        rules[0].RuleType.ShouldBe(RuleType.Exclude);
    }

    [Fact]
    public async Task when_upsert_is_called_with_existing_rule_then_rule_type_is_updated()
    {
        await SeedAccountAsync();
        _seedingContext.SyncRules.Add(new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos", RuleType = RuleType.Include });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SyncRuleRepository(_factory);
        await repository.UpsertAsync(new AccountId(AccountIdString), "/Photos", RuleType.Exclude, TestContext.Current.CancellationToken);

        var rule = await _seedingContext.SyncRules.AsNoTracking().SingleAsync(r => r.AccountId == new AccountId(AccountIdString) && r.RemotePath == "/Photos", TestContext.Current.CancellationToken);
        rule.RuleType.ShouldBe(RuleType.Exclude);
    }

    [Fact]
    public async Task when_get_by_account_id_is_called_then_only_rules_for_that_account_are_returned()
    {
        await SeedAccountAsync();
        await SeedAccountAsync("user-2");

        _seedingContext.SyncRules.AddRange(
            new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos", RuleType = RuleType.Include },
            new SyncRuleEntity { AccountId = new AccountId("user-2"), RemotePath = "/Documents", RuleType = RuleType.Include });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SyncRuleRepository(_factory);
        var rules = await repository.GetByAccountIdAsync(new AccountId(AccountIdString), TestContext.Current.CancellationToken);

        rules.Count.ShouldBe(1);
        rules[0].RemotePath.ShouldBe("/Photos");
    }

    [Fact]
    public async Task when_delete_is_called_then_the_rule_is_removed()
    {
        await SeedAccountAsync();
        _seedingContext.SyncRules.Add(new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos", RuleType = RuleType.Include });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SyncRuleRepository(_factory);
        await repository.DeleteAsync(new AccountId(AccountIdString), "/Photos", TestContext.Current.CancellationToken);

        var rules = await _seedingContext.SyncRules.AsNoTracking().Where(r => r.AccountId == new AccountId(AccountIdString)).ToListAsync(TestContext.Current.CancellationToken);
        rules.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_delete_child_rules_is_called_then_child_paths_are_removed_and_parent_is_kept()
    {
        await SeedAccountAsync();
        _seedingContext.SyncRules.AddRange(
            new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos", RuleType = RuleType.Include },
            new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos/Holidays", RuleType = RuleType.Exclude },
            new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos/Holidays/Summer", RuleType = RuleType.Exclude });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SyncRuleRepository(_factory);
        await repository.DeleteChildRulesAsync(new AccountId(AccountIdString), "/Photos", TestContext.Current.CancellationToken);

        var rules = await _seedingContext.SyncRules.AsNoTracking().Where(r => r.AccountId == new AccountId(AccountIdString)).ToListAsync(TestContext.Current.CancellationToken);
        rules.Count.ShouldBe(1);
        rules[0].RemotePath.ShouldBe("/Photos");
    }

    [Fact]
    public async Task when_delete_child_rules_is_called_then_rules_for_other_accounts_are_not_removed()
    {
        await SeedAccountAsync();
        await SeedAccountAsync("user-2");
        _seedingContext.SyncRules.AddRange(
            new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = "/Photos/Holidays", RuleType = RuleType.Exclude },
            new SyncRuleEntity { AccountId = new AccountId("user-2"), RemotePath = "/Photos/Holidays", RuleType = RuleType.Exclude });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SyncRuleRepository(_factory);
        await repository.DeleteChildRulesAsync(new AccountId(AccountIdString), "/Photos", TestContext.Current.CancellationToken);

        var remaining = await _seedingContext.SyncRules.AsNoTracking().Where(r => r.AccountId == new AccountId("user-2")).ToListAsync(TestContext.Current.CancellationToken);
        remaining.Count.ShouldBe(1);
    }

    private async Task SeedAccountAsync(string accountId = AccountIdString)
    {
        if(await _seedingContext.Accounts.AnyAsync(a => a.Id == new AccountId(accountId), TestContext.Current.CancellationToken))
            return;

        _seedingContext.Accounts.Add(new AccountEntity { Id = new AccountId(accountId), Email = $"{accountId}@test.com", DisplayName = accountId });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
