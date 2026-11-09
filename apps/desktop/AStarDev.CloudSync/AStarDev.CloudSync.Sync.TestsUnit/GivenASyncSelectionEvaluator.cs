using AStarDev.CloudSync.Accounts;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncSelectionEvaluator
{
    private readonly SyncSelectionEvaluator evaluator = new();

    [Fact]
    public void when_there_are_no_rules_then_the_folder_is_uncovered()
    {
        evaluator.Evaluate(Path("Docs"), []).ShouldBe(SelectionStateFactory.CreateUncovered());
    }

    [Fact]
    public void when_a_folder_has_an_include_rule_then_it_is_included_in_that_pair_and_direction()
    {
        var state = evaluator.Evaluate(Path("Docs"), [Include("Docs", SyncDirectionFactory.CreateUploadOnly())]);

        state.ShouldBe(SelectionStateFactory.CreateIncluded(PairAt("Docs"), SyncDirectionFactory.CreateUploadOnly()));
    }

    [Fact]
    public void when_a_folder_is_below_an_include_rule_then_it_is_included_in_the_same_pair()
    {
        var state = evaluator.Evaluate(Path("Docs/Sub/Deep"), [Include("Docs")]);

        state.ShouldBe(SelectionStateFactory.CreateIncluded(PairAt("Docs"), SyncDirectionFactory.CreateTwoWay()));
    }

    [Theory]
    [InlineData("Docs2")]
    [InlineData("Docs2/Sub")]
    [InlineData("Doc")]
    [InlineData("Other/Docs")]
    public void when_a_rule_only_shares_a_name_prefix_or_a_nested_name_then_it_does_not_match(string path)
    {
        evaluator.Evaluate(Path(path), [Include("Docs")]).ShouldBe(SelectionStateFactory.CreateUncovered());
    }

    [Theory]
    [InlineData("docs")]
    [InlineData("DOCS/sub")]
    public void when_the_folder_differs_from_the_rule_only_in_case_then_the_rule_matches(string path)
    {
        evaluator.Evaluate(Path(path), [Include("Docs")]).ShouldBeOfType<SelectionState.Included>();
    }

    [Fact]
    public void when_an_exclude_and_an_include_are_on_the_same_path_then_the_exclude_wins()
    {
        evaluator.Evaluate(Path("Docs"), [Include("Docs"), Exclude("docs")]).ShouldBe(SelectionStateFactory.CreateExcluded());
        evaluator.Evaluate(Path("Docs"), [Exclude("Docs"), Include("docs")]).ShouldBe(SelectionStateFactory.CreateExcluded());
    }

    [Fact]
    public void when_rules_nest_then_the_most_specific_one_wins()
    {
        var rules = new[] { Include("Docs"), Exclude("Docs/Private") };

        evaluator.Evaluate(Path("Docs/Private/Taxes"), rules).ShouldBe(SelectionStateFactory.CreateExcluded());
        evaluator.Evaluate(Path("Docs/Public"), rules).ShouldBe(SelectionStateFactory.CreateIncluded(PairAt("Docs"), SyncDirectionFactory.CreateTwoWay()));
    }

    [Fact]
    public void when_an_include_sits_inside_an_excluded_folder_then_it_forms_its_own_pair()
    {
        var rules = new[] { Exclude("Docs"), Include("Docs/Keep", SyncDirectionFactory.CreateDownloadOnly()) };

        evaluator.Evaluate(Path("Docs/Keep/Sub"), rules).ShouldBe(SelectionStateFactory.CreateIncluded(PairAt("Docs/Keep"), SyncDirectionFactory.CreateDownloadOnly()));
    }

    [Fact]
    public void when_an_include_sits_inside_a_pair_below_an_excluded_folder_then_it_keeps_the_pairs_direction()
    {
        var rules = new[] { Include("Docs", SyncDirectionFactory.CreateUploadOnly()), Exclude("Docs/X"), Include("Docs/X/Y", SyncDirectionFactory.CreateTwoWay()) };

        evaluator.Evaluate(Path("Docs/X/Y/Z"), rules).ShouldBe(SelectionStateFactory.CreateIncluded(PairAt("Docs"), SyncDirectionFactory.CreateUploadOnly()));
    }

    [Fact]
    public void when_a_stored_rule_below_differs_from_the_folder_then_it_is_partial()
    {
        var rules = new[] { Include("Docs"), Exclude("Docs/Private") };

        evaluator.Evaluate(Path("Docs"), rules).ShouldBe(SelectionStateFactory.CreatePartiallyIncluded());
    }

    [Fact]
    public void when_the_differing_rule_is_deep_and_its_ancestors_were_never_loaded_then_the_folder_is_still_partial()
    {
        var rules = new[] { Include("Docs"), Exclude("Docs/A/B/C/D") };

        evaluator.Evaluate(Path("Docs"), rules).ShouldBe(SelectionStateFactory.CreatePartiallyIncluded());
        evaluator.Evaluate(Path("Docs/A"), rules).ShouldBe(SelectionStateFactory.CreatePartiallyIncluded());
        evaluator.Evaluate(Path("Docs/A/B/C"), rules).ShouldBe(SelectionStateFactory.CreatePartiallyIncluded());
    }

    [Fact]
    public void when_an_uncovered_folder_has_an_include_rule_below_then_it_is_partial()
    {
        var rules = new[] { Include("Docs/Work") };

        evaluator.Evaluate(Path("Docs"), rules).ShouldBe(SelectionStateFactory.CreatePartiallyIncluded());
        evaluator.Evaluate(LocalRelativePathFactory.Root, rules).ShouldBe(SelectionStateFactory.CreatePartiallyIncluded());
    }

    [Fact]
    public void when_the_rules_below_agree_with_the_folder_then_it_is_not_partial()
    {
        evaluator.Evaluate(Path("Docs"), [Include("Docs"), Include("Docs/Sub")]).ShouldBe(SelectionStateFactory.CreateIncluded(PairAt("Docs"), SyncDirectionFactory.CreateTwoWay()));
        evaluator.Evaluate(Path("Docs"), [Exclude("Docs"), Exclude("Docs/Sub")]).ShouldBe(SelectionStateFactory.CreateExcluded());
    }

    [Fact]
    public void when_a_rule_is_on_the_root_then_it_covers_every_folder()
    {
        var rules = new[] { Include(string.Empty) };

        evaluator.Evaluate(Path("Any/Where"), rules).ShouldBe(SelectionStateFactory.CreateIncluded(PairAt(string.Empty), SyncDirectionFactory.CreateTwoWay()));
    }

    [Fact]
    public void when_the_same_account_and_path_are_used_then_the_pair_id_is_the_same_ignoring_case()
    {
        (PairAt("Docs") == PairAt("DOCS")).ShouldBeTrue();
        (PairAt("Docs") == PairAt("Docs/Sub")).ShouldBeFalse();
        (SyncPairIdFactory.CreateForRule(AccountIdFactory.Create(Guid.NewGuid()), Path("Docs")) == PairAt("Docs")).ShouldBeFalse();
    }

    [Fact]
    public void when_a_folder_is_inside_a_pair_then_its_enclosing_direction_is_the_pairs_even_below_an_exclude()
    {
        var rules = new[] { Include("Docs", SyncDirectionFactory.CreateUploadOnly()), Exclude("Docs/X"), Include("Docs/X/Y", SyncDirectionFactory.CreateTwoWay()) };

        evaluator.EnclosingDirection(Path("Docs/X"), rules).Match(direction => direction, () => SyncDirectionFactory.CreateTwoWay()).ShouldBe(SyncDirectionFactory.CreateUploadOnly());
        evaluator.EnclosingDirection(Path("Docs/X/Y/Z"), rules).Match(direction => direction, () => SyncDirectionFactory.CreateTwoWay()).ShouldBe(SyncDirectionFactory.CreateUploadOnly());
    }

    [Fact]
    public void when_no_include_encloses_a_folder_then_there_is_no_enclosing_direction()
    {
        var rules = new[] { Exclude("Docs"), Include("Docs/Keep") };

        evaluator.EnclosingDirection(Path("Docs"), rules).Match(_ => true, () => false).ShouldBeFalse();
        evaluator.EnclosingDirection(Path("Other"), rules).Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public void when_the_engine_asks_whether_a_folder_syncs_then_only_the_covering_rule_decides()
    {
        var rules = new[] { Include("Docs"), Exclude("Docs/Private") };

        (evaluator.IsIncluded(Path("Docs"), rules), evaluator.IsIncluded(Path("Docs/Public"), rules), evaluator.IsIncluded(Path("Docs/Private"), rules), evaluator.IsIncluded(Path("Other"), rules)).ShouldBe((true, true, false, false));
    }
}
