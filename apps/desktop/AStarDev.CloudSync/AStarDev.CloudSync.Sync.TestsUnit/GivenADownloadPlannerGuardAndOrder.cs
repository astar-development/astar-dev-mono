using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADownloadPlannerGuardAndOrder
{
    private static DownloadScenario Pair(DownloadScenario scenario, string folder, int baselineItems, int deletedRemotely)
    {
        for (var index = 0; index < baselineItems; index++)
        {
            scenario.BaselineFile($"{folder}/f{index}.txt", $"{folder}-{index}", "c1");
            if (index >= deletedRemotely)
                scenario.RemoteFile($"{folder}/f{index}.txt", $"{folder}-{index}", "c1");
        }

        return scenario;
    }

    [Fact]
    public void when_a_pair_loses_more_than_a_quarter_and_at_least_fifty_items_then_its_deletions_are_held()
    {
        var scenario = Pair(new DownloadScenario(), "Docs", 100, 60).RemoteFile("Docs/new.txt", "new", "c1");

        var plan = scenario.Plan();

        plan.Items.Select(DownloadScenario.Describe).ShouldBe(["download Docs/new.txt"]);
        plan.Held.Count.ShouldBe(1);
        plan.Held[0].PairId.ShouldBe(PairAt("Docs"));
        plan.Held[0].Paths.Count.ShouldBe(60);
        plan.Held[0].Total.ShouldBe(100);
    }

    [Fact]
    public void when_a_pair_loses_just_under_the_threshold_then_its_deletions_run()
    {
        var plan = Pair(new DownloadScenario(), "Docs", 100, 49).Plan();

        plan.Held.ShouldBeEmpty();
        plan.Items.Count(item => item is DownloadPlanItem.DeleteLocal).ShouldBe(49);
    }

    [Fact]
    public void when_a_pair_loses_exactly_a_quarter_then_its_deletions_run()
    {
        var plan = Pair(new DownloadScenario(), "Docs", 200, 50).Plan();

        plan.Held.ShouldBeEmpty();
        plan.Items.Count(item => item is DownloadPlanItem.DeleteLocal).ShouldBe(50);
    }

    [Fact]
    public void when_a_pair_loses_a_quarter_plus_one_then_its_deletions_are_held()
    {
        var plan = Pair(new DownloadScenario(), "Docs", 200, 51).Plan();

        plan.Held.Count.ShouldBe(1);
        plan.Items.ShouldBeEmpty();
    }

    [Fact]
    public void when_only_one_of_two_pairs_loses_too_much_then_only_that_pair_is_held()
    {
        var scenario = new DownloadScenario().Rules(Include("Docs"), Include("Pics"));
        _ = Pair(scenario, "Docs", 100, 60);
        _ = Pair(scenario, "Pics", 100, 3);

        var plan = scenario.Plan();

        plan.Held.Select(held => held.PairId).ShouldBe([PairAt("Docs")]);
        plan.Items.Count(item => item is DownloadPlanItem.DeleteLocal).ShouldBe(3);
    }

    [Fact]
    public void when_a_pair_is_held_then_its_moves_out_of_the_selection_are_held_too_but_its_downloads_still_run()
    {
        var scenario = Pair(new DownloadScenario(), "Docs", 100, 59).BaselineFile("Docs/gone.txt", "gone", "c1").RemoteFile("Elsewhere/gone.txt", "gone", "c1").RemoteFile("Docs/f1.txt", "Docs-1", "c2");

        var plan = scenario.Plan();

        plan.Held.Count.ShouldBe(1);
        plan.Items.Select(DownloadScenario.Describe).ShouldBe(["download Docs/f1.txt"]);
    }

    [Fact]
    public void when_overlapping_rules_cover_a_path_then_it_is_planned_once()
    {
        var scenario = new DownloadScenario().Rules(Include("Docs"), Include("Docs/Sub")).RemoteFile("Docs/Sub/a.txt", "a", "c1");

        scenario.Describe().ShouldBe(["download Docs/Sub/a.txt"]);
    }

    [Fact]
    public void when_the_index_lists_one_item_under_two_casings_then_one_item_is_planned()
    {
        var scenario = new DownloadScenario().RemoteFile("Docs/Report.txt", "a", "c1").RemoteFile("docs/report.TXT", "a", "c1");

        scenario.Describe().Count.ShouldBe(1);
    }

    [Fact]
    public void when_two_items_differ_only_in_case_then_one_item_is_planned()
    {
        var scenario = new DownloadScenario().RemoteFile("Docs/Report.txt", "a", "c1").RemoteFile("Docs/REPORT.txt", "b", "c1").LocalAdded("Docs/report.txt");

        scenario.Describe().Select(line => line[..^"Report.txt".Length].ToUpperInvariant()).ShouldBe(["CONFLICT BOTHADDED DOCS/"]);
    }

    [Fact]
    public void when_the_plan_has_several_kinds_then_folders_come_first_parents_before_children_and_deletions_come_last_children_first()
    {
        var scenario = new DownloadScenario()
            .RemoteFolder("Docs/A/B", "b").RemoteFolder("Docs/A", "a")
            .RemoteFile("Docs/A/B/x.txt", "x", "c1")
            .BaselineFile("Docs/m.txt", "m", "c1").RemoteFile("Docs/n.txt", "m", "c1")
            .BaselineFolder("Docs/Gone", "g").BaselineFile("Docs/Gone/y.txt", "y", "c1")
            .BaselineFile("Docs/k.txt", "k", "c1").RemoteFile("Docs/k.txt", "k", "c2").LocalModified("Docs/k.txt");

        scenario.Describe().ShouldBe(["folder Docs/A", "folder Docs/A/B", "move Docs/m.txt -> Docs/n.txt", "download Docs/A/B/x.txt", "conflict BothModified Docs/k.txt", "delete Docs/Gone/y.txt", "delete Docs/Gone"]);
    }

    [Fact]
    public void when_the_inputs_are_missing_then_the_plan_is_empty()
    {
        using var provider = new ServiceCollection().AddCloudSync().BuildServiceProvider();
        var planner = provider.GetRequiredService<IDownloadPlanner>();

        var plan = planner.Plan(DownloadPlanRequestFactory.Create(null!, null!, null!, null!, null!, null!));

        plan.Items.ShouldBeEmpty();
        plan.Held.ShouldBeEmpty();
    }
}
