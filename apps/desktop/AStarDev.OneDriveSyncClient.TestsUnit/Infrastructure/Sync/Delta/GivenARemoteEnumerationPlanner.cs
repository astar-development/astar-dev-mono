using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Delta;

public sealed class GivenARemoteEnumerationPlanner
{
    private const string DeltaLink = "https://graph.microsoft.com/v1.0/drives/d/items/root/delta(token='abc')";
    private const string Fingerprint = "fp-1";

    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    private static SyncRuleEntity Include(string path, string? itemId = "folder-1")
        => new() { AccountId = new AccountId("user-1"), RemotePath = path, RuleType = RuleType.Include, RemoteItemId = itemId is null ? Option.None<string>() : Option.Some(itemId) };

    private static EnumerationPlan Plan(Option<string>? deltaLink = null, Option<string>? storedFingerprint = null, string currentFingerprint = Fingerprint, Option<DateTimeOffset>? lastFull = null, IReadOnlyList<SyncRuleEntity>? rules = null)
        => RemoteEnumerationPlanner.Plan(deltaLink ?? Option.Some(DeltaLink), storedFingerprint ?? Option.Some(Fingerprint), currentFingerprint, lastFull ?? Option.Some(Now.AddHours(-1)), Now, MaxAge, rules ?? [Include("/A")]);

    [Fact]
    public void when_everything_is_current_then_a_change_check_is_planned_with_the_stored_delta_link()
        => Plan().ShouldBeOfType<CheckForChanges>().DeltaLink.ShouldBe(DeltaLink);

    [Fact]
    public void when_there_is_no_delta_link_then_a_walk_is_required()
        => Plan(deltaLink: Option.None<string>()).ShouldBeOfType<WalkRequired>().Reason.ShouldBe(WalkReasons.NoDeltaLink);

    [Fact]
    public void when_there_is_no_stored_fingerprint_then_a_walk_is_required()
        => Plan(storedFingerprint: Option.None<string>()).ShouldBeOfType<WalkRequired>().Reason.ShouldBe(WalkReasons.RulesChanged);

    [Fact]
    public void when_the_fingerprint_has_changed_then_a_walk_is_required()
        => Plan(currentFingerprint: "fp-2").ShouldBeOfType<WalkRequired>().Reason.ShouldBe(WalkReasons.RulesChanged);

    [Fact]
    public void when_no_full_enumeration_has_completed_then_a_walk_is_required()
        => Plan(lastFull: Option.None<DateTimeOffset>()).ShouldBeOfType<WalkRequired>().Reason.ShouldBe(WalkReasons.EnumerationTooOld);

    [Fact]
    public void when_the_last_full_enumeration_is_older_than_the_maximum_age_then_a_walk_is_required()
        => Plan(lastFull: Option.Some(Now.AddHours(-25))).ShouldBeOfType<WalkRequired>().Reason.ShouldBe(WalkReasons.EnumerationTooOld);

    [Fact]
    public void when_an_include_rule_has_no_remote_item_id_then_a_walk_is_required()
        => Plan(rules: [Include("/A"), Include("/B", itemId: null)]).ShouldBeOfType<WalkRequired>().Reason.ShouldBe(WalkReasons.RuleWithoutItemId);

    [Fact]
    public void when_an_exclude_rule_has_no_remote_item_id_then_a_change_check_is_still_planned()
    {
        var exclude = new SyncRuleEntity { AccountId = new AccountId("user-1"), RemotePath = "/A/Skip", RuleType = RuleType.Exclude };

        _ = Plan(rules: [Include("/A"), exclude]).ShouldBeOfType<CheckForChanges>();
    }
}
