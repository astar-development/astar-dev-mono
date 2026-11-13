using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;
using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPlanner : IDisposable
{
    private readonly UploadPlanFixture fixture = new();

    private static string Describe(UploadPlanItem item) => item switch
    {
        UploadPlanItem.EnsureRemoteFolder => $"EnsureRemoteFolder {item.Path.Value}",
        UploadPlanItem.UploadNewFile => $"UploadNew {item.Path.Value}",
        UploadPlanItem.UploadReplace replace => $"UploadReplace {item.Path.Value} {replace.ItemId.Value}",
        UploadPlanItem.RemoveRemote remove => $"RemoveRemote {item.Path.Value} {remove.ItemId.Value}",
        UploadPlanItem.LinkExisting link => $"LinkExisting {item.Path.Value} {link.ItemId.Value}",
        UploadPlanItem.ForgetBaseline => $"ForgetBaseline {item.Path.Value}",
        UploadPlanItem.SendToConflicts conflict => $"SendToConflicts {item.Path.Value} {conflict.Kind.GetType().Name}",
        UploadPlanItem.Rejected rejected => $"Rejected {item.Path.Value} {rejected.Reason.GetType().Name}",
        _ => "unknown",
    };

    private static string[] Steps(UploadPlan plan) => [.. plan.Items.Select(Describe)];

    [Fact]
    public void when_a_new_file_has_nothing_remote_then_it_is_uploaded_with_a_must_not_exist_precondition()
    {
        var plan = fixture.AddedFile("Docs/a.txt", 42).Plan();

        var item = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.UploadNewFile>();
        item.Size.ShouldBe(42);
        item.Precondition.ShouldBe(UploadPreconditionFactory.CreateMustNotExist());
    }

    [Fact]
    public void when_a_new_file_already_exists_remotely_without_a_baseline_then_it_goes_to_conflicts_as_both_added()
    {
        var plan = fixture.AddedFile("Docs/a.txt").RemoteFile("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt BothAdded"]);
    }

    [Fact]
    public void when_a_new_folder_has_nothing_remote_then_the_remote_folder_is_ensured()
    {
        var plan = fixture.AddedFolder("Docs/New").Plan();

        Steps(plan).ShouldBe(["EnsureRemoteFolder Docs/New"]);
    }

    [Fact]
    public void when_a_new_folder_already_exists_remotely_then_it_is_linked_without_an_upload()
    {
        var plan = fixture.AddedFolder("Docs/New").RemoteFolder("Docs/New", "rf1").Plan();

        Steps(plan).ShouldBe(["LinkExisting Docs/New rf1"]);
    }

    [Fact]
    public void when_a_new_folder_meets_a_remote_file_of_the_same_name_then_it_goes_to_conflicts_as_both_added()
    {
        var plan = fixture.AddedFolder("Docs/New").RemoteFile("Docs/New", "r1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/New BothAdded"]);
    }

    [Fact]
    public void when_a_file_is_modified_and_the_remote_tag_is_unchanged_then_it_is_replaced_with_a_must_match_precondition()
    {
        var plan = fixture.ModifiedFile("Docs/a.txt").BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "r1", "c1").Plan();

        var item = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.UploadReplace>();
        item.ItemId.ShouldBe(Id("r1"));
        item.Precondition.ShouldBe(UploadPreconditionFactory.CreateMustMatch(new ContentTag("c1")).Match(precondition => precondition, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void when_a_file_is_modified_and_the_remote_tag_changed_then_it_goes_to_conflicts_as_both_modified()
    {
        var plan = fixture.ModifiedFile("Docs/a.txt").BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "r1", "c2").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt BothModified"]);
    }

    [Fact]
    public void when_a_file_is_modified_and_the_remote_copy_is_gone_then_it_goes_to_conflicts_as_local_modified_remote_deleted()
    {
        var plan = fixture.ModifiedFile("Docs/a.txt").BaselineFile("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt LocalModifiedRemoteDeleted"]);
    }

    [Fact]
    public void when_a_file_is_modified_and_the_remote_index_marks_it_deleted_then_it_goes_to_conflicts_as_local_modified_remote_deleted()
    {
        var plan = fixture.ModifiedFile("Docs/a.txt").BaselineFile("Docs/a.txt", "r1").RemoteDeleted("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt LocalModifiedRemoteDeleted"]);
    }

    [Fact]
    public void when_a_file_is_modified_and_another_remote_item_took_its_path_then_it_goes_to_conflicts_as_both_modified()
    {
        var plan = fixture.ModifiedFile("Docs/a.txt").BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "other", "c1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt BothModified"]);
    }

    [Fact]
    public void when_a_file_is_modified_without_a_baseline_row_then_it_goes_to_conflicts_as_both_added()
    {
        var plan = fixture.ModifiedFile("Docs/a.txt").RemoteFile("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt BothAdded"]);
    }

    [Fact]
    public void when_a_file_is_deleted_and_the_remote_tag_is_unchanged_then_the_remote_item_is_removed_with_the_baseline_content_tag()
    {
        var plan = fixture.DeletedItem("Docs/a.txt", "r1").BaselineFile("Docs/a.txt", "r1", "c7", "e7").RemoteFile("Docs/a.txt", "r1", "c7", "e9").Plan();

        var item = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.RemoveRemote>();
        item.ItemId.ShouldBe(Id("r1"));
        item.Precondition.ShouldBe(RemovePreconditionFactory.CreateMustMatchContent(new ContentTag("c7")).Match(precondition => precondition, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void when_a_file_is_deleted_and_the_remote_copy_changed_then_it_goes_to_conflicts_as_local_deleted_remote_modified()
    {
        var plan = fixture.DeletedItem("Docs/a.txt", "r1").BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "r1", "c2").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt LocalDeletedRemoteModified"]);
    }

    [Fact]
    public void when_a_file_is_deleted_and_the_remote_copy_is_already_gone_then_only_the_baseline_row_is_forgotten()
    {
        var plan = fixture.DeletedItem("Docs/a.txt", "r1").BaselineFile("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["ForgetBaseline Docs/a.txt"]);
    }

    [Fact]
    public void when_a_file_is_deleted_and_the_remote_index_marks_it_deleted_then_only_the_baseline_row_is_forgotten()
    {
        var plan = fixture.DeletedItem("Docs/a.txt", "r1").BaselineFile("Docs/a.txt", "r1").RemoteDeleted("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["ForgetBaseline Docs/a.txt"]);
    }

    [Fact]
    public void when_a_file_is_deleted_without_a_baseline_row_then_only_the_baseline_row_is_forgotten()
    {
        var plan = fixture.DeletedItem("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["ForgetBaseline Docs/a.txt"]);
    }

    [Fact]
    public void when_a_file_is_deleted_and_the_remote_path_holds_another_item_then_it_goes_to_conflicts()
    {
        var plan = fixture.DeletedItem("Docs/a.txt", "r1").BaselineFile("Docs/a.txt", "r1", "c1").RemoteFile("Docs/a.txt", "other", "c1").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/a.txt LocalDeletedRemoteModified"]);
    }

    [Fact]
    public void when_a_folder_is_deleted_and_everything_below_it_is_known_then_the_folder_is_removed()
    {
        var plan = fixture.DeletedItem("Docs/Old", "rf1").BaselineFolder("Docs/Old", "rf1").BaselineFile("Docs/Old/a.txt", "r1", "c1")
            .RemoteFolder("Docs/Old", "rf1", "fe5").RemoteFile("Docs/Old/a.txt", "r1", "c1").Plan();

        var item = plan.Items.ShouldHaveSingleItem().ShouldBeOfType<UploadPlanItem.RemoveRemote>();
        item.ItemId.ShouldBe(Id("rf1"));
        item.Precondition.ShouldBe(RemovePreconditionFactory.CreateMustMatch(new EntityTag("fe5")).Match(precondition => precondition, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void when_a_folder_is_deleted_and_a_new_remote_file_sits_below_it_then_it_goes_to_conflicts()
    {
        var plan = fixture.DeletedItem("Docs/Old", "rf1").BaselineFolder("Docs/Old", "rf1")
            .RemoteFolder("Docs/Old", "rf1").RemoteFile("Docs/Old/new.txt", "r9").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/Old LocalDeletedRemoteModified"]);
    }

    [Fact]
    public void when_a_folder_is_deleted_and_a_remote_file_below_it_changed_then_it_goes_to_conflicts()
    {
        var plan = fixture.DeletedItem("Docs/Old", "rf1").BaselineFolder("Docs/Old", "rf1").BaselineFile("Docs/Old/a.txt", "r1", "c1")
            .RemoteFolder("Docs/Old", "rf1").RemoteFile("Docs/Old/a.txt", "r1", "c2").Plan();

        Steps(plan).ShouldBe(["SendToConflicts Docs/Old LocalDeletedRemoteModified"]);
    }

    [Fact]
    public void when_a_folder_is_deleted_and_a_remote_item_below_it_is_only_marked_deleted_then_the_folder_is_still_removed()
    {
        var plan = fixture.DeletedItem("Docs/Old", "rf1").BaselineFolder("Docs/Old", "rf1")
            .RemoteFolder("Docs/Old", "rf1").RemoteDeleted("Docs/Old/gone.txt", "r9").Plan();

        Steps(plan).ShouldBe(["RemoveRemote Docs/Old rf1"]);
    }

    [Fact]
    public void when_a_change_is_deferred_or_unreadable_then_nothing_is_planned_for_it()
    {
        var plan = fixture.DeferredFile("Docs/a.txt").UnreadableFile("Docs/b.txt").Plan();

        plan.Items.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_same_path_is_reported_twice_then_one_step_is_planned()
    {
        var plan = fixture.AddedFile("Docs/a.txt").AddedFile("Docs/a.txt").Plan();

        Steps(plan).ShouldBe(["UploadNew Docs/a.txt"]);
    }

    [Fact]
    public void when_paths_differ_only_by_case_then_one_step_is_planned()
    {
        var plan = fixture.AddedFile("Docs/a.txt").ModifiedFile("docs/A.TXT").BaselineFile("Docs/a.txt", "r1").RemoteFile("Docs/a.txt", "r1").Plan();

        plan.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void when_include_rules_overlap_then_each_path_still_gets_one_step()
    {
        fixture.Rules = [Include("Docs"), Include("Docs/Sub", SyncDirectionFactory.CreateUploadOnly())];

        var plan = fixture.AddedFile("Docs/Sub/a.txt").AddedFile("docs/sub/A.txt").AddedFile("Docs/b.txt").Plan();

        Steps(plan).Order(StringComparer.Ordinal).ShouldBe(["UploadNew Docs/Sub/a.txt", "UploadNew Docs/b.txt"]);
    }

    [Fact]
    public void when_a_path_is_outside_every_include_rule_then_nothing_is_planned()
    {
        var plan = fixture.AddedFile("Other/a.txt").AddedFolder("Other/New").Plan();

        plan.Items.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_path_is_excluded_then_nothing_is_planned()
    {
        fixture.Rules = [Include("Docs"), Exclude("Docs/Private")];

        var plan = fixture.AddedFile("Docs/Private/a.txt").AddedFile("Docs/b.txt").Plan();

        Steps(plan).ShouldBe(["UploadNew Docs/b.txt"]);
    }

    [Fact]
    public void when_a_pair_is_download_only_then_nothing_is_uploaded_for_it()
    {
        fixture.Rules = [Include("Docs", SyncDirectionFactory.CreateDownloadOnly())];

        var plan = fixture.AddedFile("Docs/a.txt").AddedFolder("Docs/New").Plan();

        plan.Items.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_pair_is_upload_only_then_its_changes_are_planned()
    {
        fixture.Rules = [Include("Docs", SyncDirectionFactory.CreateUploadOnly())];

        var plan = fixture.AddedFile("Docs/a.txt").Plan();

        Steps(plan).ShouldBe(["UploadNew Docs/a.txt"]);
    }

    [Fact]
    public void when_a_folder_is_a_rule_folder_itself_then_its_deletion_is_planned()
    {
        var plan = fixture.DeletedItem("Docs", "rf1").BaselineFolder("Docs", "rf1").RemoteFolder("Docs", "rf1").Plan();

        Steps(plan).ShouldBe(["RemoveRemote Docs rf1"]);
    }

    [Fact]
    public void when_the_plan_has_every_kind_of_step_then_folders_come_first_parents_before_children_then_files_then_deletions_children_first_then_reported_items()
    {
        fixture.AddedFile("Docs/New/Deep/c.txt").AddedFolder("Docs/New/Deep").AddedFolder("Docs/New").AddedFile("Docs/b.txt")
            .DeletedItem("Docs/Old", "rf1").BaselineFolder("Docs/Old", "rf1").RemoteFolder("Docs/Old", "rf1")
            .DeletedItem("Docs/Old/x.txt", "r2").BaselineFile("Docs/Old/x.txt", "r2", "c1").RemoteFile("Docs/Old/x.txt", "r2", "c1")
            .BaselineFile("Docs/Old/x.txt", "r2", "c1")
            .AddedFile("Docs/clash.txt").RemoteFile("Docs/clash.txt", "r3");
        fixture.Names.Rejected.Add("bad.txt");

        var plan = fixture.AddedFile("Docs/bad.txt").Plan();

        Steps(plan).ShouldBe([
            "EnsureRemoteFolder Docs/New",
            "EnsureRemoteFolder Docs/New/Deep",
            "UploadNew Docs/b.txt",
            "UploadNew Docs/New/Deep/c.txt",
            "RemoveRemote Docs/Old/x.txt r2",
            "RemoveRemote Docs/Old rf1",
            "Rejected Docs/bad.txt ReservedName",
            "SendToConflicts Docs/clash.txt BothAdded",
        ]);
    }

    [Fact]
    public void when_a_file_is_renamed_then_the_new_copy_is_uploaded_before_the_old_one_is_removed()
    {
        var plan = fixture.DeletedItem("Docs/old.txt", "r1").AddedFile("Docs/new.txt").BaselineFile("Docs/old.txt", "r1", "c1").RemoteFile("Docs/old.txt", "r1", "c1").Plan();

        Steps(plan).ShouldBe(["UploadNew Docs/new.txt", "RemoveRemote Docs/old.txt r1"]);
    }

    [Fact]
    public void when_the_name_policy_rejects_a_new_file_then_it_is_reported_and_not_uploaded()
    {
        fixture.Names.Rejected.Add("a:b.txt");

        var plan = fixture.AddedFile("Docs/a:b.txt").Plan();

        Steps(plan).ShouldBe(["Rejected Docs/a:b.txt ReservedName"]);
    }

    [Fact]
    public void when_the_name_policy_rejects_a_modified_file_then_it_is_reported_and_not_replaced()
    {
        fixture.Names.Rejected.Add("a.txt");

        var plan = fixture.ModifiedFile("Docs/a.txt").BaselineFile("Docs/a.txt", "r1").RemoteFile("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["Rejected Docs/a.txt ReservedName"]);
    }

    [Fact]
    public void when_the_name_policy_rejects_a_new_folder_then_it_is_reported_and_not_created()
    {
        fixture.Names.Rejected.Add("New");

        var plan = fixture.AddedFolder("Docs/New").Plan();

        Steps(plan).ShouldBe(["Rejected Docs/New ReservedName"]);
    }

    [Fact]
    public void when_the_name_policy_is_asked_then_it_gets_the_last_segment_and_the_file_size_and_zero_for_a_folder()
    {
        _ = fixture.AddedFile("Docs/Sub/a.txt", 123).AddedFolder("Docs/Sub").Plan();

        fixture.Names.Checks.Select(check => $"{check.Name}:{check.Size}").Order(StringComparer.Ordinal).ShouldBe(["Sub:0", "a.txt:123"]);
    }

    [Fact]
    public void when_a_name_is_rejected_and_the_path_exists_remotely_then_the_rejection_wins_over_the_conflict()
    {
        fixture.Names.Rejected.Add("a.txt");

        var plan = fixture.AddedFile("Docs/a.txt").RemoteFile("Docs/a.txt", "r1").Plan();

        Steps(plan).ShouldBe(["Rejected Docs/a.txt ReservedName"]);
    }

    public void Dispose() => fixture.Dispose();
}
