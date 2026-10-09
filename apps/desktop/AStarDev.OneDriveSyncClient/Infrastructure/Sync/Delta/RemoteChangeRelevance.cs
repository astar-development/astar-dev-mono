using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Decides whether a set of remote changes can affect the items currently being synced. Anything ambiguous is treated as relevant.</summary>
public static class RemoteChangeRelevance
{
    /// <summary>Returns true when any change touches a synced item, an include-rule root, or a direct child of either; or when a changed item's parent is unknown.</summary>
    public static bool AnyAffectSyncedScope(IReadOnlyList<DeltaChange> changes, IReadOnlyDictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<SyncRuleEntity> rules)
    {
        HashSet<string> ruleRootIds = new(rules.Where(rule => rule.RuleType == RuleType.Include).Select(rule => rule.RemoteItemId).OfType<Option<string>.Some>().Select(some => some.Value), StringComparer.OrdinalIgnoreCase);

        return changes.Any(change => IsRelevant(change, syncedItems, ruleRootIds));
    }

    private static bool IsRelevant(DeltaChange change, IReadOnlyDictionary<string, SyncedItemEntity> syncedItems, HashSet<string> ruleRootIds)
        => change switch
        {
            ItemDeleted deleted => syncedItems.ContainsKey(deleted.ItemId),
            ItemChanged changed => syncedItems.ContainsKey(changed.ItemId) || ruleRootIds.Contains(changed.ItemId) || IsInScope(changed.ParentId, syncedItems, ruleRootIds),
            _ => true
        };

    private static bool IsInScope(Option<string> parentId, IReadOnlyDictionary<string, SyncedItemEntity> syncedItems, HashSet<string> ruleRootIds)
        => parentId is not Option<string>.Some parent || syncedItems.ContainsKey(parent.Value) || ruleRootIds.Contains(parent.Value);
}
