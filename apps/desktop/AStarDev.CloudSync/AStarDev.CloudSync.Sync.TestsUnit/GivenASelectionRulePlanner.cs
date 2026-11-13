using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASelectionRulePlanner
{
    private static readonly SyncDirection TwoWay = SyncDirectionFactory.CreateTwoWay();

    [Fact]
    public void when_a_folder_is_included_then_an_include_rule_is_added_with_its_remote_id_and_direction()
    {
        var remoteId = RemoteItemIdFactory.Create("remote-1").Match(Option.Some, _ => Option.None<RemoteItemId>());

        var rules = SelectionRulePlanner.Apply(Account, [], [SelectionChangeFactory.CreateIncludeFolder(Path("Docs"), remoteId, SyncDirectionFactory.CreateUploadOnly())]);

        rules.ShouldBe([SelectionRuleFactory.Create(Account, Path("Docs"), remoteId, SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateUploadOnly()))]);
    }

    [Fact]
    public void when_a_folder_is_included_then_every_rule_below_it_and_on_it_is_dropped()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Exclude("Docs/Private"), Include("Docs/Sub"), Exclude("Docs"), Exclude("Other")], [SelectionChangeFactory.CreateIncludeFolder(Path("Docs"), Option.None<RemoteItemId>(), TwoWay)]);

        rules.ShouldBe([Include("Docs"), Exclude("Other")]);
    }

    [Fact]
    public void when_a_folder_is_excluded_then_an_exclude_rule_is_added_and_every_rule_below_it_is_dropped()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs"), Include("Docs/Sub"), Include("Docs2")], [SelectionChangeFactory.CreateExcludeFolder(Path("Docs"), Option.None<RemoteItemId>())]);

        rules.ShouldBe([Exclude("Docs"), Include("Docs2")]);
    }

    [Fact]
    public void when_a_folder_under_an_existing_pair_is_included_then_the_rule_is_added()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs"), Exclude("Docs/Private")], [SelectionChangeFactory.CreateIncludeFolder(Path("Docs/Private"), Option.None<RemoteItemId>(), TwoWay)]);

        rules.ShouldBe([Include("Docs"), Include("Docs/Private")]);
    }

    [Fact]
    public void when_the_direction_of_a_pair_changes_then_only_its_top_most_include_rule_changes()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs"), Exclude("Docs/X"), Include("Docs/X/Y"), Include("Other")], [SelectionChangeFactory.CreateChangeDirection(PairAt("Docs"), SyncDirectionFactory.CreateDownloadOnly())]);

        rules.ShouldBe([Include("Docs", SyncDirectionFactory.CreateDownloadOnly()), Exclude("Docs/X"), Include("Docs/X/Y"), Include("Other")]);
    }

    [Fact]
    public void when_the_pair_is_unknown_then_a_direction_change_changes_nothing()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs")], [SelectionChangeFactory.CreateChangeDirection(PairAt("Missing"), SyncDirectionFactory.CreateUploadOnly())]);

        rules.ShouldBe([Include("Docs")]);
    }

    [Fact]
    public void when_rules_differ_only_in_case_then_one_rule_per_path_is_kept_and_the_last_wins()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("docs"), Exclude("Docs")], []);

        rules.Count.ShouldBe(1);
        rules[0].Kind.ShouldBe(SelectionRuleKindFactory.CreateExclude());
    }

    [Fact]
    public void when_changes_are_applied_then_the_rules_are_sorted_by_path_ignoring_case()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("zeta"), Include("Alpha")], [SelectionChangeFactory.CreateIncludeFolder(Path("beta"), Option.None<RemoteItemId>(), TwoWay)]);

        rules.Select(rule => rule.Path.Value).ShouldBe(["Alpha", "beta", "zeta"]);
    }

    [Fact]
    public void when_several_changes_touch_one_folder_then_they_apply_in_order()
    {
        var rules = SelectionRulePlanner.Apply(Account, [], [SelectionChangeFactory.CreateIncludeFolder(Path("Docs"), Option.None<RemoteItemId>(), TwoWay), SelectionChangeFactory.CreateExcludeFolder(Path("Docs"), Option.None<RemoteItemId>())]);

        rules.ShouldBe([Exclude("Docs")]);
    }

    [Fact]
    public void when_a_name_only_shares_a_prefix_then_its_rule_is_not_dropped()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs2")], [SelectionChangeFactory.CreateExcludeFolder(Path("Docs"), Option.None<RemoteItemId>())]);

        rules.ShouldBe([Exclude("Docs"), Include("Docs2")]);
    }

    [Fact]
    public void when_a_folder_is_relocated_then_its_rule_moves_to_the_new_path_and_keeps_its_kind_and_remote_id()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs", SyncDirectionFactory.CreateUploadOnly(), "remote-1"), Exclude("Other")], [Relocate(("docs", "Papers"))]);

        rules.ShouldBe([Exclude("Other"), Include("Papers", SyncDirectionFactory.CreateUploadOnly(), "remote-1")]);
    }

    [Fact]
    public void when_the_new_path_is_held_by_a_rule_that_stays_then_the_existing_rule_wins_and_the_moved_rule_is_dropped()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs", remoteId: "remote-1"), Exclude("papers")], [Relocate(("Docs", "Papers"))]);

        rules.ShouldBe([Exclude("papers")]);
    }

    [Fact]
    public void when_no_rule_sits_at_the_old_path_then_a_relocation_changes_nothing()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs")], [Relocate(("Music", "Songs"))]);

        rules.ShouldBe([Include("Docs")]);
    }

    [Fact]
    public void when_a_folder_is_relocated_then_the_rules_below_it_are_left_for_their_own_relocation()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs"), Exclude("Docs/Private")], [Relocate(("Docs", "Papers"))]);

        rules.ShouldBe([Exclude("Docs/Private"), Include("Papers")]);
    }

    [Fact]
    public void when_two_folders_swap_paths_then_both_rules_follow_their_folders()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("A", remoteId: "a"), Exclude("B", "b")], [Relocate(("A", "B"), ("B", "A"))]);

        rules.ShouldBe([Exclude("A", "b"), Include("B", remoteId: "a")]);
    }

    [Fact]
    public void when_one_folder_moves_onto_the_old_path_of_another_that_moves_on_then_neither_rule_is_dropped()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("A", remoteId: "a"), Exclude("B", "b")], [Relocate(("A", "B"), ("B", "C"))]);

        rules.ShouldBe([Include("B", remoteId: "a"), Exclude("C", "b")]);
    }

    [Fact]
    public void when_two_moves_aim_at_one_path_then_the_first_by_path_wins_and_the_other_is_dropped()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("A", remoteId: "a"), Exclude("B", "b")], [Relocate(("A", "X"), ("B", "x"))]);

        rules.ShouldBe([Include("X", remoteId: "a")]);
    }

    [Fact]
    public void when_a_move_keeps_the_same_path_ignoring_case_then_the_rule_is_unchanged()
    {
        var rules = SelectionRulePlanner.Apply(Account, [Include("Docs")], [Relocate(("Docs", "docs"))]);

        rules.ShouldBe([Include("Docs")]);
    }

    private static SelectionChange Relocate(params (string Old, string New)[] moves) => SelectionChangeFactory.CreateRelocateFolders(moves.Select(move => FolderMoveFactory.Create(Path(move.Old), Path(move.New))));
}
