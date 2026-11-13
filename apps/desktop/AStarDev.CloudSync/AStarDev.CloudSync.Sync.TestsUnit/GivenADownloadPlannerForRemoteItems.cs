using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADownloadPlannerForRemoteItems
{
    [Fact]
    public void when_a_new_remote_file_has_nothing_local_then_it_is_downloaded()
    {
        var scenario = new DownloadScenario().RemoteFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBe(["download Docs/a.txt"]);
    }

    [Fact]
    public void when_a_new_remote_folder_has_nothing_local_then_it_is_created()
    {
        var scenario = new DownloadScenario().RemoteFolder("Docs/Sub", "s");

        scenario.Describe().ShouldBe(["folder Docs/Sub"]);
    }

    [Fact]
    public void when_a_new_remote_file_meets_a_local_addition_then_it_goes_to_conflicts_as_both_added()
    {
        var scenario = new DownloadScenario().RemoteFile("Docs/a.txt", "a", "c1").LocalAdded("Docs/a.txt");

        scenario.Describe().ShouldBe(["conflict BothAdded Docs/a.txt"]);
    }

    [Fact]
    public void when_a_new_remote_folder_meets_a_local_folder_then_the_folder_is_linked()
    {
        var scenario = new DownloadScenario().RemoteFolder("Docs/Sub", "s").LocalAdded("Docs/Sub", folder: true);

        scenario.Describe().ShouldBe(["folder Docs/Sub"]);
    }

    [Fact]
    public void when_a_new_remote_folder_meets_a_local_file_then_it_goes_to_conflicts_as_both_added()
    {
        var scenario = new DownloadScenario().RemoteFolder("Docs/Sub", "s").LocalAdded("Docs/Sub");

        scenario.Describe().ShouldBe(["conflict BothAdded Docs/Sub"]);
    }

    [Fact]
    public void when_the_remote_content_tag_differs_and_the_local_file_is_unchanged_then_it_is_downloaded_again()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2");

        scenario.Describe().ShouldBe(["download Docs/a.txt"]);
    }

    [Fact]
    public void when_the_remote_content_tag_differs_and_the_local_file_was_modified_then_it_goes_to_conflicts_as_both_modified()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").LocalModified("Docs/a.txt");

        scenario.Describe().ShouldBe(["conflict BothModified Docs/a.txt"]);
    }

    [Fact]
    public void when_the_remote_content_tag_differs_and_the_local_file_was_deleted_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").LocalDeleted("Docs/a.txt", "a");

        scenario.Describe().ShouldBe(["conflict LocalDeletedRemoteModified Docs/a.txt"]);
    }

    [Fact]
    public void when_the_remote_content_tag_differs_and_the_local_file_is_still_changing_then_it_waits_without_a_conflict()
    {
        var deferred = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").LocalDeferred("Docs/a.txt");
        var unreadable = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").LocalUnreadable("Docs/a.txt");

        deferred.Describe().ShouldBeEmpty();
        unreadable.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_nothing_changed_on_either_side_then_nothing_is_planned()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c1").BaselineFolder("Docs/Sub", "s").RemoteFolder("Docs/Sub", "s");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_the_content_tag_is_the_same_and_only_the_local_file_changed_then_nothing_is_downloaded()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c1").LocalModified("Docs/a.txt");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_the_content_tag_is_the_same_but_the_file_is_missing_locally_then_it_is_repaired()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c1").MissingLocally("Docs/a.txt");

        scenario.Describe().ShouldBe(["download Docs/a.txt"]);
    }

    [Fact]
    public void when_a_missing_file_is_held_by_the_guard_then_it_is_not_repaired()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c1").MissingLocally("Docs/a.txt").Quarantined("Docs/a.txt", PathQuarantineStateFactory.CreateHeldByGuard());

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_resync_is_running_then_a_missing_file_is_not_repaired_but_a_new_file_is_downloaded()
    {
        var scenario = new DownloadScenario().Resyncing().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c1").MissingLocally("Docs/a.txt").RemoteFile("Docs/b.txt", "b", "c1");

        scenario.Describe().ShouldBe(["download Docs/b.txt"]);
    }

    [Fact]
    public void when_a_path_has_an_unresolved_conflict_then_nothing_is_planned_for_it()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/a.txt", "a", "c2").Quarantined("Docs/a.txt", PathQuarantineStateFactory.CreateConflicted());

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_path_is_skipped_then_nothing_is_planned_for_it()
    {
        var scenario = new DownloadScenario().RemoteFile("Docs/.hidden/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_the_pair_is_upload_only_then_nothing_is_downloaded()
    {
        var scenario = new DownloadScenario().Rules(Include("Docs", SyncDirectionFactory.CreateUploadOnly())).RemoteFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_the_pair_is_download_only_then_files_are_downloaded()
    {
        var scenario = new DownloadScenario().Rules(Include("Docs", SyncDirectionFactory.CreateDownloadOnly())).RemoteFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBe(["download Docs/a.txt"]);
    }

    [Fact]
    public void when_a_remote_item_is_outside_the_selection_then_it_is_not_downloaded()
    {
        var scenario = new DownloadScenario().RemoteFile("Other/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_new_remote_file_replaces_a_different_baseline_item_at_the_path_then_it_is_downloaded()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "old", "c1").RemoteFile("Docs/a.txt", "new", "c9");

        scenario.Describe().ShouldBe(["download Docs/a.txt"]);
    }

    [Fact]
    public void when_a_remote_folder_replaces_a_baseline_file_at_the_path_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a", "old", "c1").RemoteFolder("Docs/a", "new");

        scenario.Describe().ShouldBe(["conflict BothModified Docs/a"]);
    }

    [Fact]
    public void when_the_remote_kind_differs_from_the_baseline_kind_for_the_same_item_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a", "a", "c1").RemoteFolder("Docs/a", "a");

        scenario.Describe().ShouldBe(["conflict BothModified Docs/a"]);
    }

    [Fact]
    public void when_a_new_remote_file_meets_a_local_modification_of_a_replaced_item_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "old", "c1").RemoteFile("Docs/a.txt", "new", "c9").LocalModified("Docs/a.txt");

        scenario.Describe().ShouldBe(["conflict BothModified Docs/a.txt"]);
    }

    [Fact]
    public void when_a_new_remote_file_meets_a_local_deletion_of_a_replaced_item_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "old", "c1").RemoteFile("Docs/a.txt", "new", "c9").LocalDeleted("Docs/a.txt", "old");

        scenario.Describe().ShouldBe(["conflict LocalDeletedRemoteModified Docs/a.txt"]);
    }

    [Fact]
    public void when_a_new_remote_file_meets_a_path_that_is_unreadable_then_it_waits()
    {
        var scenario = new DownloadScenario().RemoteFile("Docs/a.txt", "a", "c1").LocalUnreadable("Docs/a.txt");

        scenario.Describe().ShouldBeEmpty();
    }
}
