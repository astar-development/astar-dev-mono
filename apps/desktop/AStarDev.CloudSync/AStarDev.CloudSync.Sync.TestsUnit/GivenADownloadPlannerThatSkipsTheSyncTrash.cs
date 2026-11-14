using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADownloadPlannerThatSkipsTheSyncTrash
{
    private static DownloadScenario Scenario() => new DownloadScenario().Rules(Include("Docs"), Include(".sync-trash"), Include(".sync-trash-notes"));

    [Fact]
    public void when_a_remote_file_is_in_the_sync_trash_then_it_is_not_planned()
    {
        var scenario = Scenario().RemoteFile(".sync-trash/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_the_sync_trash_name_differs_in_casing_then_it_is_not_planned()
    {
        var scenario = Scenario().RemoteFile(".SYNC-TRASH/a.txt", "a", "c1").RemoteFile(".Sync-Trash/b.txt", "b", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_remote_file_is_nested_below_the_sync_trash_then_it_is_not_planned()
    {
        var scenario = Scenario().RemoteFile(".sync-trash/20261113120000000-abc/Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_remote_folder_is_named_exactly_the_sync_trash_then_it_is_not_created()
    {
        var scenario = Scenario().RemoteFolder(".sync-trash", "t");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_the_sync_trash_name_is_not_the_root_segment_then_the_dot_prefix_rule_still_skips_the_item()
    {
        var scenario = Scenario().RemoteFile("Docs/.sync-trash/b.txt", "b", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_root_folder_only_starts_with_the_sync_trash_name_then_the_dot_prefix_rule_still_skips_its_files()
    {
        var scenario = Scenario().RemoteFile(".sync-trash-notes/n.txt", "n", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_trash_and_normal_items_overlap_then_only_the_normal_ones_are_planned()
    {
        var scenario = Scenario().RemoteFile("Docs/a.txt", "a", "c1").RemoteFile(".sync-trash/Docs/a.txt", "t1", "c1").RemoteFile(".SYNC-TRASH/Docs/a.txt", "t2", "c1");

        scenario.Describe().ShouldBe(["download Docs/a.txt"]);
    }

    [Fact]
    public void when_a_synced_item_now_appears_only_in_the_sync_trash_then_the_unchanged_local_copy_is_deleted()
    {
        var scenario = Scenario().BaselineFile("Docs/a.txt", "a", "c1").BaselineFile("Docs/b.txt", "b", "c1").BaselineFile("Docs/c.txt", "c", "c1").BaselineFile("Docs/d.txt", "d", "c1").RemoteFile("Docs/b.txt", "b", "c1").RemoteFile("Docs/c.txt", "c", "c1").RemoteFile("Docs/d.txt", "d", "c1").RemoteFile(".sync-trash/20261113120000000-abc/a.txt", "a", "c1");

        scenario.Describe().ShouldBe(["delete Docs/a.txt"]);
    }

    [Fact]
    public void when_a_synced_item_now_appears_only_in_the_sync_trash_and_was_modified_locally_then_it_goes_to_conflicts()
    {
        var scenario = Scenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile(".sync-trash/20261113120000000-abc/a.txt", "a", "c1").LocalModified("Docs/a.txt");

        scenario.Describe().ShouldBe(["conflict LocalModifiedRemoteDeleted Docs/a.txt"]);
    }

    [Fact]
    public void when_a_baseline_row_sits_under_the_sync_trash_then_it_is_never_deleted_or_downloaded()
    {
        var scenario = Scenario().BaselineFile(".sync-trash/20261113120000000-abc/a.txt", "a", "c1").RemoteFile(".sync-trash/20261113120000000-abc/a.txt", "a", "c2");

        scenario.Describe().ShouldBeEmpty();
    }
}
