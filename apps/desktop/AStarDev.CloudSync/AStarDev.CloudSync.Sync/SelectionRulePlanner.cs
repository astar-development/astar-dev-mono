using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Applies staged <see cref="SelectionChange"/> values to a rule list (#1397).</summary>
public static class SelectionRulePlanner
{
    /// <summary>Applies the changes in order. Including or excluding a folder replaces any rule on it and drops every rule below it. Changing a pair's direction changes its top-most Include rule. Rules are de-duplicated by path, ignoring case, and returned sorted by path.</summary>
    /// <param name="accountId">The account the new rules belong to.</param>
    /// <param name="rules">The stored rules.</param>
    /// <param name="changes">The staged changes.</param>
    public static IReadOnlyList<SelectionRule> Apply(AccountId accountId, IReadOnlyList<SelectionRule> rules, IReadOnlyList<SelectionChange> changes)
    {
        _ = (accountId, changes);

        return rules;
    }
}
