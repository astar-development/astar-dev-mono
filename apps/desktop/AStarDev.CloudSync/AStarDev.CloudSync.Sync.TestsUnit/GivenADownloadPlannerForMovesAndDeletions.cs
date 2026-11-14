using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADownloadPlannerForMovesAndDeletions
{
    [Fact]
    public void when_a_remote_file_was_renamed_and_the_local_file_is_unchanged_then_it_is_moved_without_a_download()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c1");

        scenario.Describe().ShouldBe(["move Docs/a.txt -> Docs/b.txt"]);
    }

    [Fact]
    public void when_a_remote_file_was_renamed_and_changed_then_only_the_move_is_planned_and_the_next_pass_downloads_it()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c2");

        scenario.Describe().ShouldBe(["move Docs/a.txt -> Docs/b.txt"]);
    }

    [Fact]
    public void when_a_remote_file_was_renamed_and_the_local_file_was_modified_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c1").LocalModified("Docs/a.txt");

        scenario.Describe().ShouldBe(["conflict BothModified Docs/a.txt"]);
    }

    [Fact]
    public void when_a_remote_file_was_renamed_and_the_local_file_was_deleted_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c1").LocalDeleted("Docs/a.txt", "a");

        scenario.Describe().ShouldBe(["conflict LocalDeletedRemoteModified Docs/a.txt"]);
    }

    [Fact]
    public void when_a_remote_file_was_renamed_and_the_local_file_is_still_changing_then_it_waits()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c1").LocalDeferred("Docs/a.txt");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_remote_file_was_renamed_onto_a_local_addition_then_it_goes_to_conflicts_as_move_target_taken()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c1").LocalAdded("Docs/b.txt");

        scenario.Describe().ShouldBe(["conflict MoveTargetTaken Docs/b.txt"]);
    }

    [Fact]
    public void when_a_move_target_is_taken_then_the_kind_comes_from_the_classifier_with_a_move_target_signal()
    {
        var classifier = new RecordingConflictClassifier(ConflictKindFactory.CreateBothAdded());
        var scenario = new DownloadScenario().ClassifiedBy(classifier).BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Docs/b.txt", "a", "c1").LocalAdded("Docs/b.txt");

        scenario.Describe().ShouldBe(["conflict BothAdded Docs/b.txt"]);
        classifier.Signals.ShouldContain(signal => signal is ConflictSignal.MoveTargetInUse);
    }

    [Fact]
    public void when_a_remote_file_was_renamed_onto_another_baseline_item_then_it_goes_to_conflicts_as_move_target_taken()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").BaselineFile("Docs/b.txt", "b", "c1").RemoteFile("Docs/b.txt", "a", "c1").RemoteFile("Docs/c.txt", "b", "c1");

        scenario.Describe().ShouldContain("conflict MoveTargetTaken Docs/b.txt");
    }

    [Fact]
    public void when_a_remote_folder_with_many_files_was_renamed_then_one_move_is_planned_for_the_folder()
    {
        var scenario = new DownloadScenario()
            .BaselineFolder("Docs/Old", "f").RemoteFolder("Docs/New", "f")
            .BaselineFile("Docs/Old/a.txt", "a", "c1").RemoteFile("Docs/New/a.txt", "a", "c1")
            .BaselineFile("Docs/Old/b.txt", "b", "c1").RemoteFile("Docs/New/b.txt", "b", "c1")
            .BaselineFolder("Docs/Old/Sub", "s").RemoteFolder("Docs/New/Sub", "s")
            .BaselineFile("Docs/Old/Sub/c.txt", "c", "c1").RemoteFile("Docs/New/Sub/c.txt", "c", "c1");

        scenario.Describe().ShouldBe(["move Docs/Old -> Docs/New"]);
    }

    [Fact]
    public void when_a_renamed_remote_folder_has_a_local_change_inside_then_only_the_folder_goes_to_conflicts()
    {
        var scenario = new DownloadScenario()
            .BaselineFolder("Docs/Old", "f").RemoteFolder("Docs/New", "f")
            .BaselineFile("Docs/Old/a.txt", "a", "c1").RemoteFile("Docs/New/a.txt", "a", "c1")
            .BaselineFile("Docs/Old/b.txt", "b", "c1").RemoteFile("Docs/New/b.txt", "b", "c1")
            .LocalModified("Docs/Old/a.txt");

        scenario.Describe().ShouldContain("conflict BothModified Docs/Old");
        scenario.Describe().Where(line => line.StartsWith("move", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_renamed_remote_folder_has_a_file_still_changing_inside_then_nothing_is_planned_for_it()
    {
        var scenario = new DownloadScenario().BaselineFolder("Docs/Old", "f").RemoteFolder("Docs/New", "f").BaselineFile("Docs/Old/a.txt", "a", "c1").RemoteFile("Docs/New/a.txt", "a", "c1").LocalDeferred("Docs/Old/a.txt");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_remote_item_was_deleted_and_the_local_item_is_unchanged_then_it_is_deleted_locally()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBe(["delete Docs/a.txt"]);
    }

    [Fact]
    public void when_a_remote_item_was_deleted_and_the_local_file_was_modified_then_it_goes_to_conflicts()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").LocalModified("Docs/a.txt");

        scenario.Describe().ShouldBe(["conflict LocalModifiedRemoteDeleted Docs/a.txt"]);
    }

    [Fact]
    public void when_a_remote_item_was_deleted_and_the_local_item_is_gone_too_then_nothing_is_planned()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").LocalDeleted("Docs/a.txt", "a");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_remote_item_was_deleted_and_the_local_file_is_still_changing_then_it_waits()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").LocalDeferred("Docs/a.txt");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_remote_item_moved_out_of_the_selection_and_the_local_item_is_unchanged_then_it_is_deleted_locally()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").RemoteFile("Elsewhere/a.txt", "a", "c1");

        scenario.Describe().ShouldBe(["delete Docs/a.txt"]);
    }

    [Fact]
    public void when_a_folder_is_unticked_then_its_local_copies_are_kept()
    {
        var scenario = new DownloadScenario().Rules(Include("Other")).BaselineFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_deletion_is_held_by_the_guard_path_then_it_is_not_planned()
    {
        var scenario = new DownloadScenario().BaselineFile("Docs/a.txt", "a", "c1").Quarantined("Docs/a.txt", PathQuarantineStateFactory.CreateHeldByGuard());

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_resync_is_running_then_a_missing_remote_item_is_not_deleted_locally()
    {
        var scenario = new DownloadScenario().Resyncing().BaselineFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }

    [Fact]
    public void when_a_deleted_remote_folder_has_a_new_local_file_inside_then_the_folder_is_not_deleted()
    {
        var scenario = new DownloadScenario().BaselineFolder("Docs/Sub", "s").BaselineFile("Docs/Sub/a.txt", "a", "c1").LocalAdded("Docs/Sub/new.txt");

        scenario.Describe().ShouldBe(["delete Docs/Sub/a.txt"]);
    }

    [Fact]
    public void when_a_pair_is_upload_only_then_remote_deletions_are_not_applied_locally()
    {
        var scenario = new DownloadScenario().Rules(Include("Docs", SyncDirectionFactory.CreateUploadOnly())).BaselineFile("Docs/a.txt", "a", "c1");

        scenario.Describe().ShouldBeEmpty();
    }
}
