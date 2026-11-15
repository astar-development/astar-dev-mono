using CsCheck;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPlanForAnyEdits
{
    private static readonly string[] Names = ["Docs/a.txt", "Docs/b.txt", "Docs/c.txt"];

    private static readonly Gen<(int Remote, int Baseline, int Local)[]> AnyEdits = Gen.Select(Gen.Int[0, 2], Gen.Int[0, 1], Gen.Int[0, 3]).Array[Names.Length];

    private static UploadPlan PlanOf((int Remote, int Baseline, int Local)[] edits)
    {
        using var fixture = new UploadPlanFixture();
        for (var index = 0; index < Names.Length; index++)
            Apply(fixture, Names[index], $"id{index}", edits[index]);

        return fixture.Plan();
    }

    private static void Apply(UploadPlanFixture fixture, string path, string id, (int Remote, int Baseline, int Local) edit)
    {
        if (edit.Baseline == 1)
            _ = fixture.BaselineFile(path, id);

        if (edit.Remote > 0)
            _ = fixture.RemoteFile(path, id, $"c{edit.Remote}");

        _ = edit.Local switch
        {
            1 => fixture.AddedFile(path),
            2 => fixture.ModifiedFile(path),
            3 => fixture.DeletedItem(path, id),
            _ => fixture,
        };
    }

    private static IEnumerable<(string Name, (int Remote, int Baseline, int Local) Edit)> Pairs((int Remote, int Baseline, int Local)[] edits) => Names.Select((name, index) => (name, edits[index]));

    [Fact]
    public void when_the_remote_changed_since_the_baseline_then_a_local_edit_never_replaces_or_removes_it()
    {
        AnyEdits.Sample(edits =>
        {
            var plan = PlanOf(edits);
            var changedRemotely = Pairs(edits).Where(entry => entry.Edit is { Baseline: 1, Remote: 2 }).Select(entry => entry.Name).ToList();

            return !plan.Items.Any(item => item is UploadPlanItem.UploadReplace or UploadPlanItem.RemoveRemote && changedRemotely.Contains(item.Path.Value!));
        });
    }

    [Fact]
    public void when_a_remote_file_already_sits_at_a_locally_added_path_then_it_is_never_overwritten()
    {
        AnyEdits.Sample(edits =>
        {
            var plan = PlanOf(edits);
            var occupied = Pairs(edits).Where(entry => entry.Edit is { Remote: > 0, Local: 1 }).Select(entry => entry.Name).ToList();

            return !plan.Items.Any(item => item is UploadPlanItem.UploadNewFile or UploadPlanItem.UploadReplace && occupied.Contains(item.Path.Value!));
        });
    }

    [Fact]
    public void when_a_local_edit_meets_a_remote_change_then_it_goes_to_conflicts()
    {
        AnyEdits.Sample(edits =>
        {
            var plan = PlanOf(edits);
            var conflicted = plan.Items.OfType<UploadPlanItem.SendToConflicts>().Select(item => item.Path.Value).ToList();

            return Pairs(edits).Where(entry => entry.Edit is { Baseline: 1, Remote: 2, Local: 2 or 3 }).All(entry => conflicted.Contains(entry.Name));
        });
    }

    [Fact]
    public void when_any_edits_are_planned_then_there_is_at_most_one_item_per_path()
    {
        AnyEdits.Sample(edits =>
        {
            var paths = PlanOf(edits).Items.Select(item => item.Path.Value!.ToUpperInvariant()).ToList();

            return paths.Count == paths.Distinct().Count();
        });
    }
}
