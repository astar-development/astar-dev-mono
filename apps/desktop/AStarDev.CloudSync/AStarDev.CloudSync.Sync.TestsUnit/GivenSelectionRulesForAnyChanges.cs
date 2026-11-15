using AStarDev.FunctionalParadigm;
using CsCheck;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenSelectionRulesForAnyChanges
{
    private static readonly Gen<string> AnyFolder = Gen.Char["aAbB"].Array[1, 2].Select(letters => new string(letters)).Array[1, 3].Select(segments => string.Join('/', segments));

    private static readonly Gen<(string Path, bool Include)[]> AnyChanges = Gen.Select(AnyFolder, Gen.Bool).Array[1, 10];

    private static IReadOnlyList<SelectionRule> Applied((string Path, bool Include)[] changes) => SelectionRulePlanner.Apply(Account, [], [.. changes.Select(ToChange)]);

    private static SelectionChange ToChange((string Path, bool Include) change) => change.Include
        ? SelectionChangeFactory.CreateIncludeFolder(Path(change.Path), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay())
        : SelectionChangeFactory.CreateExcludeFolder(Path(change.Path), Option.None<RemoteItemId>());

    [Fact]
    public void when_any_changes_are_applied_then_no_two_rules_share_a_path_ignoring_case()
    {
        AnyChanges.Sample(changes =>
        {
            var paths = Applied(changes).Select(rule => rule.Path.Value!.ToUpperInvariant()).ToList();

            return paths.Count == paths.Distinct().Count();
        });
    }

    [Fact]
    public void when_any_changes_are_applied_then_the_rules_are_sorted_by_path()
    {
        AnyChanges.Sample(changes =>
        {
            var paths = Applied(changes).Select(rule => rule.Path.Value!).ToList();

            return paths.SequenceEqual(paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void when_a_folder_is_included_or_excluded_last_then_it_has_that_rule_and_nothing_below_it_is_kept()
    {
        AnyChanges.Sample(changes =>
        {
            var last = changes[^1];
            var rules = Applied(changes);
            var own = rules.Where(rule => string.Equals(rule.Path.Value, last.Path, StringComparison.OrdinalIgnoreCase)).ToList();
            var below = rules.Where(rule => rule.Path.Value!.StartsWith(last.Path + "/", StringComparison.OrdinalIgnoreCase)).ToList();

            return own.Count == 1 && (own[0].Kind is SelectionRuleKind.Include) == last.Include && below.Count == 0;
        });
    }

    [Fact]
    public void when_the_result_is_applied_with_no_changes_then_it_is_unchanged()
    {
        AnyChanges.Sample(changes =>
        {
            var rules = Applied(changes);

            return SelectionRulePlanner.Apply(Account, rules, []).SequenceEqual(rules);
        });
    }
}
