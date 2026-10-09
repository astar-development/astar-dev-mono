using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Delta;

public sealed class GivenARulesFingerprintCalculator
{
    private static SyncRuleEntity Rule(string path, RuleType type = RuleType.Include, string? itemId = null)
        => new() { AccountId = new AccountId("user-1"), RemotePath = path, RuleType = type, RemoteItemId = itemId is null ? Option.None<string>() : Option.Some(itemId) };

    [Fact]
    public void when_the_same_rules_are_supplied_in_a_different_order_then_the_fingerprint_is_the_same()
    {
        string first = RulesFingerprintCalculator.Compute([Rule("/A"), Rule("/B", RuleType.Exclude)]);
        string second = RulesFingerprintCalculator.Compute([Rule("/B", RuleType.Exclude), Rule("/A")]);

        second.ShouldBe(first);
    }

    [Fact]
    public void when_a_rule_path_differs_only_by_case_then_the_fingerprint_is_the_same()
    {
        string first = RulesFingerprintCalculator.Compute([Rule("/Photos")]);
        string second = RulesFingerprintCalculator.Compute([Rule("/photos")]);

        second.ShouldBe(first);
    }

    [Fact]
    public void when_a_rule_is_added_then_the_fingerprint_changes()
    {
        string first = RulesFingerprintCalculator.Compute([Rule("/A")]);
        string second = RulesFingerprintCalculator.Compute([Rule("/A"), Rule("/A/Sub", RuleType.Exclude)]);

        second.ShouldNotBe(first);
    }

    [Fact]
    public void when_a_rule_changes_from_include_to_exclude_then_the_fingerprint_changes()
    {
        string first = RulesFingerprintCalculator.Compute([Rule("/A")]);
        string second = RulesFingerprintCalculator.Compute([Rule("/A", RuleType.Exclude)]);

        second.ShouldNotBe(first);
    }

    [Fact]
    public void when_only_the_remote_item_id_is_backfilled_then_the_fingerprint_is_the_same()
    {
        string first = RulesFingerprintCalculator.Compute([Rule("/A")]);
        string second = RulesFingerprintCalculator.Compute([Rule("/A", itemId: "folder-1")]);

        second.ShouldBe(first);
    }

    [Fact]
    public void when_there_are_no_rules_then_a_non_empty_fingerprint_is_returned()
        => RulesFingerprintCalculator.Compute([]).ShouldNotBeNullOrEmpty();
}
