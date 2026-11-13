namespace AStarDev.CloudSync.Sync;

internal static class RuleRelocations
{
    internal static (IReadOnlyList<SelectionRule> Rules, int Dropped) Apply(IReadOnlyList<SelectionRule> rules, IReadOnlyList<FolderMove> moves)
    {
        var targets = moves.Where(move => !move.OldPath.Equals(move.NewPath)).GroupBy(move => move.OldPath).ToDictionary(group => group.Key, group => group.First().NewPath);
        var staying = rules.Where(rule => !targets.ContainsKey(rule.Path)).ToList();
        var taken = new HashSet<LocalRelativePath>(staying.Select(rule => rule.Path));
        var moved = new List<SelectionRule>();
        var dropped = 0;

        foreach (var rule in rules.Where(rule => targets.ContainsKey(rule.Path)).OrderBy(rule => rule.Path.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            var target = targets[rule.Path];

            if (taken.Add(target))
                moved.Add(SelectionRuleFactory.Create(rule.AccountId, target, rule.RemoteId, rule.Kind));
            else
                dropped++;
        }

        return ([.. staying, .. moved], dropped);
    }
}
