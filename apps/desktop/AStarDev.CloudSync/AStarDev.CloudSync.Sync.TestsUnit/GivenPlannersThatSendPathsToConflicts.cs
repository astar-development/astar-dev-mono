using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenPlannersThatSendPathsToConflicts
{
    [Fact]
    public void when_the_download_planner_finds_both_sides_modified_then_the_item_carries_both_snapshots()
    {
        var plan = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").LocalModified("Docs/a.txt").Plan();

        var finding = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<DownloadPlanItem.SendToConflicts>().Finding;

        var local = finding.Local.ShouldBeOfType<SideSnapshot.Present>();
        var remote = finding.Remote.ShouldBeOfType<SideSnapshot.Present>();
        (local.Size, local.Hash.Match(_ => true, () => false), remote.Size, remote.Tag).ShouldBe((5, true, 10, Option.Some(RemoteIds.CTag("c2"))));
    }

    [Fact]
    public void when_the_download_planner_finds_a_local_deletion_and_a_remote_edit_then_the_local_side_is_absent()
    {
        var plan = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").LocalDeleted("Docs/a.txt", "a").Plan();

        var finding = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<DownloadPlanItem.SendToConflicts>().Finding;

        finding.Local.ShouldBeOfType<SideSnapshot.Absent>();
        finding.Remote.ShouldBeOfType<SideSnapshot.Present>();
    }

    [Fact]
    public void when_the_download_planner_finds_a_local_edit_and_a_remote_deletion_then_the_remote_side_is_absent()
    {
        var plan = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").LocalModified("Docs/a.txt").Plan();

        var finding = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<DownloadPlanItem.SendToConflicts>().Finding;

        finding.Local.ShouldBeOfType<SideSnapshot.Present>();
        finding.Remote.ShouldBeOfType<SideSnapshot.Absent>();
    }

    [Fact]
    public void when_the_download_planner_finds_a_file_added_on_both_sides_then_the_item_holds_the_remote_item()
    {
        var plan = new DownloadScenario().RemoteFile("Docs/a.txt", "a", "c1").LocalAdded("Docs/a.txt").Plan();

        var item = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<DownloadPlanItem.SendToConflicts>();

        item.RemoteItem.Match(found => found.Id, () => default).ShouldBe(RemoteIds.Of("a"));
        item.Finding.Local.ShouldBeOfType<SideSnapshot.Present>().Hash.Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public void when_the_upload_planner_finds_both_sides_modified_then_the_item_carries_both_snapshots()
    {
        using var fixture = new UploadPlanFixture();

        var plan = fixture.ModifiedFile("Docs/a.txt", 20).BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "r1", "c2").Plan();

        var finding = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.SendToConflicts>().Finding;
        var local = finding.Local.ShouldBeOfType<SideSnapshot.Present>();
        var remote = finding.Remote.ShouldBeOfType<SideSnapshot.Present>();
        (local.Size, remote.Tag).ShouldBe((20, Option.Some(new ContentTag("c2"))));
    }

    [Fact]
    public void when_the_upload_planner_finds_a_local_deletion_and_a_remote_edit_then_the_local_side_is_absent()
    {
        using var fixture = new UploadPlanFixture();

        var plan = fixture.DeletedItem("Docs/a.txt", "r1").BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "r1", "c2").Plan();

        var finding = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.SendToConflicts>().Finding;
        finding.Local.ShouldBeOfType<SideSnapshot.Absent>();
        finding.Remote.ShouldBeOfType<SideSnapshot.Present>();
    }

    [Fact]
    public void when_the_upload_planner_finds_a_file_added_on_both_sides_then_the_item_holds_the_remote_item()
    {
        using var fixture = new UploadPlanFixture();

        var plan = fixture.AddedFile("Docs/a.txt", 42).RemoteFile("Docs/a.txt", "r1").Plan();

        var item = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.SendToConflicts>();
        item.RemoteItem.Match(found => found.Id.Value, () => string.Empty).ShouldBe("r1");
        item.Finding.Local.ShouldBeOfType<SideSnapshot.Present>().Size.ShouldBe(42);
    }
}
