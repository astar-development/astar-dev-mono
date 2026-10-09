using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Decides, before any Graph call, whether a full remote walk is already known to be required.</summary>
public static class RemoteEnumerationPlanner
{
    /// <summary>Returns <see cref="WalkRequired"/> unless a stored delta link exists, the rules are unchanged, a full enumeration completed within <paramref name="maxFullEnumerationAge"/> and every include rule has a resolved remote item id.</summary>
    public static EnumerationPlan Plan(Option<string> storedDeltaLink, Option<string> storedFingerprint, string currentFingerprint, Option<DateTimeOffset> lastFullEnumerationAt, DateTimeOffset now, TimeSpan maxFullEnumerationAge, IReadOnlyList<SyncRuleEntity> rules)
    {
        if (storedDeltaLink is not Option<string>.Some deltaLink)
            return EnumerationPlanFactory.CreateWalkRequired(WalkReasons.NoDeltaLink);

        if (storedFingerprint is not Option<string>.Some fingerprint || fingerprint.Value != currentFingerprint)
            return EnumerationPlanFactory.CreateWalkRequired(WalkReasons.RulesChanged);

        if (lastFullEnumerationAt is not Option<DateTimeOffset>.Some lastFull || now - lastFull.Value > maxFullEnumerationAge)
            return EnumerationPlanFactory.CreateWalkRequired(WalkReasons.EnumerationTooOld);

        if (rules.Any(rule => rule.RuleType == RuleType.Include && rule.RemoteItemId is not Option<string>.Some))
            return EnumerationPlanFactory.CreateWalkRequired(WalkReasons.RuleWithoutItemId);

        return EnumerationPlanFactory.CreateCheckForChanges(deltaLink.Value);
    }
}
