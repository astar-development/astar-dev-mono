using CsCheck;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPlanForAnyEdits
{
    private static readonly string[] Names = ["Docs/a.txt", "Docs/b.txt", "Docs/c.txt"];

    private static readonly Gen<(int Remote, int Baseline, int Local)[]> AnyEdits = Gen.Select(Gen.Int[0, 2], Gen.Int[0, 1], Gen.Int[0, 5]).Array[Names.Length];

    private static DownloadScenario ScenarioOf((int Remote, int Baseline, int Local)[] edits)
    {
        var scenario = new DownloadScenario().RemoteFolder("Docs", "docs");
        for (var index = 0; index < Names.Length; index++)
            scenario = Apply(scenario, Names[index], $"id{index}", edits[index]);

        return scenario;
    }

    private static DownloadScenario Apply(DownloadScenario scenario, string path, string id, (int Remote, int Baseline, int Local) edit)
    {
        var withBaseline = edit.Baseline == 1 ? scenario.BaselineFile(path, id, "c1") : scenario;
        var withRemote = edit.Remote == 0 ? withBaseline : withBaseline.RemoteFile(path, id, $"c{edit.Remote}");

        return edit.Local switch
        {
            1 => withRemote.LocalAdded(path),
            2 => withRemote.LocalModified(path),
            3 => withRemote.LocalDeleted(path, id),
            4 => withRemote.LocalDeferred(path),
            5 => withRemote.LocalUnreadable(path),
            _ => withRemote,
        };
    }

    private static bool TouchesUnsyncedLocalData(DownloadPlanItem item, (int Remote, int Baseline, int Local)[] edits) =>
        item is DownloadPlanItem.DownloadFile or DownloadPlanItem.DeleteLocal && Names.Select((name, index) => (name, edit: edits[index])).Any(entry => string.Equals(entry.name, item.Path.Value, StringComparison.OrdinalIgnoreCase) && entry.edit.Local is 1 or 2);

    [Fact]
    public void when_any_local_and_remote_edits_meet_then_a_locally_added_or_modified_file_is_never_overwritten_or_deleted()
    {
        AnyEdits.Sample(edits => !ScenarioOf(edits).Plan().Items.Any(item => TouchesUnsyncedLocalData(item, edits)));
    }

    [Fact]
    public void when_a_file_changed_both_locally_and_remotely_since_the_baseline_then_it_is_sent_to_conflicts()
    {
        AnyEdits.Sample(edits =>
        {
            var conflicted = ScenarioOf(edits).Plan().Items.OfType<DownloadPlanItem.SendToConflicts>().Select(item => item.Path.Value).ToList();

            return Names.Select((name, index) => (name, edit: edits[index])).Where(entry => entry.edit is { Baseline: 1, Remote: 2, Local: 2 or 3 }).All(entry => conflicted.Contains(entry.name));
        });
    }

    [Fact]
    public void when_any_local_and_remote_edits_meet_then_the_plan_has_one_item_per_path()
    {
        AnyEdits.Sample(edits =>
        {
            var paths = ScenarioOf(edits).Plan().Items.Select(item => item.Path.Value.ToUpperInvariant()).ToList();

            return paths.Count == paths.Distinct().Count();
        });
    }
}
