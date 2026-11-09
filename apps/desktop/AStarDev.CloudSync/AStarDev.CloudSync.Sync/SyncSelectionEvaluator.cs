using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class SyncSelectionEvaluator : ISyncSelectionEvaluator
{
    /// <inheritdoc />
    public SelectionState Evaluate(LocalRelativePath path, IReadOnlyList<SelectionRule> rules)
    {
        var covering = CoveringRules(path, rules);
        var own = OwnState(covering);

        return rules.Any(rule => SelectionPaths.IsStrictlyBelow(rule.Path, path) && IsInclude(rule) != own.Match(deciding => IsInclude(deciding), () => false))
            ? SelectionStateFactory.CreatePartiallyIncluded()
            : own.Match(deciding => StateOf(deciding, covering), SelectionStateFactory.CreateUncovered);
    }

    /// <inheritdoc />
    public bool IsIncluded(LocalRelativePath path, IReadOnlyList<SelectionRule> rules) => OwnState(CoveringRules(path, rules)).Match(IsInclude, () => false);

    /// <inheritdoc />
    public Option<SyncDirection> EnclosingDirection(LocalRelativePath path, IReadOnlyList<SelectionRule> rules) =>
        Option.None<SyncDirection>();

    private static List<SelectionRule> CoveringRules(LocalRelativePath path, IReadOnlyList<SelectionRule> rules) => [.. rules.Where(rule => SelectionPaths.IsAncestorOrSelf(rule.Path, path))];

    private static bool IsInclude(SelectionRule rule) => rule.Kind is SelectionRuleKind.Include;

    private static Option<SelectionRule> OwnState(List<SelectionRule> covering) =>
        covering.Count == 0
            ? Option.None<SelectionRule>()
            : Option.Some(covering.GroupBy(rule => SelectionPaths.Depth(rule.Path)).MaxBy(group => group.Key)!.OrderBy(IsInclude).First());

    private static SelectionState StateOf(SelectionRule deciding, List<SelectionRule> covering)
    {
        if (deciding.Kind is not SelectionRuleKind.Include)
            return SelectionStateFactory.CreateExcluded();

        var topMost = covering.Where(IsInclude).MinBy(rule => SelectionPaths.Depth(rule.Path))!;

        return SelectionStateFactory.CreateIncluded(SyncPairIdFactory.CreateForRule(topMost.AccountId, topMost.Path), ((SelectionRuleKind.Include)topMost.Kind).Direction);
    }
}
