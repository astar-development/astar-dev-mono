using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Rules;

public sealed class GivenASyncRuleService
{
    private const string AccountIdString = "account-1";
    private const string ParentPath = "/Photos";
    private const string ChildPath = "/Photos/Holidays";
    private const string ParentItemId = "item-parent";
    private const string ChildItemId = "item-child";

    [Fact]
    public async Task when_applying_include_rule_then_child_rules_are_deleted_first()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = BuildSut(repo);
        var accountId = new AccountId(AccountIdString);

        await sut.ApplyRuleAsync(accountId, ParentPath, RuleType.Include, [(ParentPath, ParentItemId)], CancellationToken.None);

        await repo.Received(1).DeleteChildRulesAsync(accountId, ParentPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_applying_include_rule_then_all_provided_nodes_are_upserted()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = BuildSut(repo);
        var accountId = new AccountId(AccountIdString);
        (string RemotePath, string Id)[] nodes = [(ParentPath, ParentItemId), (ChildPath, ChildItemId)];

        await sut.ApplyRuleAsync(accountId, ParentPath, RuleType.Include, nodes, CancellationToken.None);

        await repo.Received(1).UpsertAsync(accountId, ParentPath, RuleType.Include, ParentItemId, Arg.Any<CancellationToken>());
        await repo.Received(1).UpsertAsync(accountId, ChildPath, RuleType.Include, ChildItemId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_applying_exclude_rule_then_only_the_single_node_is_upserted()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = BuildSut(repo);
        var accountId = new AccountId(AccountIdString);

        await sut.ApplyRuleAsync(accountId, ParentPath, RuleType.Exclude, [(ParentPath, ParentItemId)], CancellationToken.None);

        await repo.Received(1).UpsertAsync(accountId, ParentPath, RuleType.Exclude, ParentItemId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_applying_include_rule_then_returns_count_of_include_rules()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = ParentPath, RuleType = RuleType.Include },
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = ChildPath, RuleType = RuleType.Include }
            ]);

        var sut = BuildSut(repo);
        var accountId = new AccountId(AccountIdString);

        int result = await sut.ApplyRuleAsync(accountId, ParentPath, RuleType.Include, [(ParentPath, ParentItemId)], CancellationToken.None);

        result.ShouldBe(2);
    }

    [Fact]
    public async Task when_applying_exclude_rule_then_returns_count_of_remaining_include_rules()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = ChildPath, RuleType = RuleType.Include }
            ]);

        var sut = BuildSut(repo);
        var accountId = new AccountId(AccountIdString);

        int result = await sut.ApplyRuleAsync(accountId, ParentPath, RuleType.Exclude, [(ParentPath, ParentItemId)], CancellationToken.None);

        result.ShouldBe(1);
    }

    [Fact]
    public async Task when_applying_include_rule_then_delete_is_called_before_upsert()
    {
        var callOrder = new List<string>();
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        repo.DeleteChildRulesAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("delete"); return Task.CompletedTask; });

        repo.UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("upsert"); return Task.CompletedTask; });

        var sut = BuildSut(repo);
        var accountId = new AccountId(AccountIdString);

        await sut.ApplyRuleAsync(accountId, ParentPath, RuleType.Include, [(ParentPath, ParentItemId)], CancellationToken.None);

        callOrder[0].ShouldBe("delete");
        callOrder[1].ShouldBe("upsert");
    }

    private static SyncRuleService BuildSut(ISyncRuleRepository repo)
        => new(repo, Substitute.For<ILogger<SyncRuleService>>());
}
