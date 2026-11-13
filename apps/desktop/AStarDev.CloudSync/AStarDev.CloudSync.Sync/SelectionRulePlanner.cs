using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Applies staged <see cref="SelectionChange"/> values to a rule list (#1397).</summary>
public static class SelectionRulePlanner
{
    /// <summary>Applies the changes in order. Including or excluding a folder replaces any rule on it and drops every rule below it. Changing a pair's direction changes the Include rule that starts the pair. Rules are de-duplicated by path, ignoring case, and returned sorted by path.</summary>
    /// <param name="accountId">The account the new rules belong to.</param>
    /// <param name="rules">The stored rules.</param>
    /// <param name="changes">The staged changes.</param>
    public static IReadOnlyList<SelectionRule> Apply(AccountId accountId, IReadOnlyList<SelectionRule> rules, IReadOnlyList<SelectionChange> changes) =>
        Tidy(changes.Aggregate(rules, (current, change) => ApplyOne(accountId, current, change)));

    private static IReadOnlyList<SelectionRule> ApplyOne(AccountId accountId, IReadOnlyList<SelectionRule> rules, SelectionChange change) => change switch
    {
        SelectionChange.IncludeFolder include => Replace(rules, SelectionRuleFactory.Create(accountId, include.Path, include.RemoteId, SelectionRuleKindFactory.CreateInclude(include.Direction))),
        SelectionChange.ExcludeFolder exclude => Replace(rules, SelectionRuleFactory.Create(accountId, exclude.Path, exclude.RemoteId, SelectionRuleKindFactory.CreateExclude())),
        SelectionChange.ChangeDirection direction => [.. rules.Select(rule => IsPairRule(rule, direction.Pair) ? SelectionRuleFactory.Create(rule.AccountId, rule.Path, rule.RemoteId, SelectionRuleKindFactory.CreateInclude(direction.Direction)) : rule)],
        SelectionChange.RelocateFolders => rules,
        _ => rules,
    };

    private static bool IsPairRule(SelectionRule rule, SyncPairId pair) => rule.Kind is SelectionRuleKind.Include && SyncPairIdFactory.CreateForRule(rule.AccountId, rule.Path) == pair;

    private static IReadOnlyList<SelectionRule> Replace(IReadOnlyList<SelectionRule> rules, SelectionRule added) =>
        [.. rules.Where(rule => !SelectionPaths.IsAncestorOrSelf(added.Path, rule.Path)), added];

    private static IReadOnlyList<SelectionRule> Tidy(IReadOnlyList<SelectionRule> rules) =>
        [.. rules.GroupBy(rule => rule.Path.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase).Select(group => group.Last()).OrderBy(rule => rule.Path.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)];
}
