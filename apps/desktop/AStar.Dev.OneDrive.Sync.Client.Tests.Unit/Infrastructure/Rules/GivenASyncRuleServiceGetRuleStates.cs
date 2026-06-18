using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Rules;

public sealed class GivenASyncRuleServiceGetRuleStates
{
    private const string AccountIdString = "account-1";
    private const string IncludePath     = "/Photos";
    private const string ExcludePath     = "/Videos";
    private const string MixedCasePath   = "/photos";

    [Fact]
    public async Task when_getting_rule_states_then_include_rules_are_returned()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = IncludePath, RuleType = RuleType.Include }]);

        var sut = BuildSut(repo);

        var result = await sut.GetRuleStatesAsync(new AccountId(AccountIdString), CancellationToken.None);

        result.ShouldContainKey(IncludePath);
        result[IncludePath].ShouldBe(RuleType.Include);
    }

    [Fact]
    public async Task when_getting_rule_states_then_exclude_rules_are_returned()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = ExcludePath, RuleType = RuleType.Exclude }]);

        var sut = BuildSut(repo);

        var result = await sut.GetRuleStatesAsync(new AccountId(AccountIdString), CancellationToken.None);

        result.ShouldContainKey(ExcludePath);
        result[ExcludePath].ShouldBe(RuleType.Exclude);
    }

    [Fact]
    public async Task when_getting_rule_states_with_both_rule_types_then_both_are_present()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = IncludePath, RuleType = RuleType.Include },
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = ExcludePath,  RuleType = RuleType.Exclude }
            ]);

        var sut = BuildSut(repo);

        var result = await sut.GetRuleStatesAsync(new AccountId(AccountIdString), CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_looking_up_a_path_with_different_casing_then_the_rule_is_found()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = IncludePath, RuleType = RuleType.Include }]);

        var sut = BuildSut(repo);

        var result = await sut.GetRuleStatesAsync(new AccountId(AccountIdString), CancellationToken.None);

        result.ShouldContainKey(MixedCasePath);
    }

    [Fact]
    public async Task when_there_are_no_rules_then_an_empty_dictionary_is_returned()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = BuildSut(repo);

        var result = await sut.GetRuleStatesAsync(new AccountId(AccountIdString), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    private static SyncRuleService BuildSut(ISyncRuleRepository repo)
        => new(repo, Substitute.For<ILogger<SyncRuleService>>());
}
