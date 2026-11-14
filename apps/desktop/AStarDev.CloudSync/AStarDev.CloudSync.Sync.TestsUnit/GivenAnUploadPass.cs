using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPass : IDisposable
{
    private readonly UploadPassFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string[] Outcomes(Result<UploadPassSummary, UploadPassFailure> result) =>
        result.Match(summary => summary.Outcomes.Select(outcome => $"{outcome.GetType().Name} {outcome.Path.Value}").Order(StringComparer.Ordinal).ToArray(), failure => [failure.GetType().Name]);

    [Fact]
    public async Task when_a_new_file_sits_in_a_known_folder_then_it_is_uploaded_into_that_folder_and_recorded()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Uploaded Docs/a.txt"]);
        var call = fixture.Uploader.Calls.ShouldHaveSingleItem();
        call.Target.ShouldBe(UploadTargetFactory.CreateNewFileIn(Option.Some(Id("docs-id"))));
        call.Precondition.ShouldBe(UploadPreconditionFactory.CreateMustNotExist());
        var (baseline, remote) = fixture.Recorder.Recorded.ShouldHaveSingleItem();
        (baseline.ItemId.Value, ((SyncBaselineKind.File)baseline.Kind).CTag.Value, remote.Name, remote.ParentId.Match(id => id.Value, () => "none")).ShouldBe(("id-a.txt", "c-a.txt", "a.txt", "docs-id"));
    }

    [Fact]
    public async Task when_a_new_file_sits_in_a_remote_folder_the_baseline_does_not_know_then_the_remote_index_supplies_the_parent()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Uploader.Calls.ShouldHaveSingleItem().Target.ShouldBe(UploadTargetFactory.CreateNewFileIn(Option.Some(Id("remote-docs"))));
    }

    [Fact]
    public async Task when_new_files_sit_in_a_new_folder_then_the_folder_is_created_once_and_both_files_go_into_it()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.AddedFolder("Docs/New").AddedFile("Docs/New/a.txt").AddedFile("Docs/New/b.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["FolderCreated Docs/New", "Uploaded Docs/New/a.txt", "Uploaded Docs/New/b.txt"]);
        fixture.Folders.Calls.Select(call => call.Name).ShouldBe(["New"]);
        fixture.Uploader.Calls.Select(call => call.Target).ShouldAllBe(target => target.Equals(UploadTargetFactory.CreateNewFileIn(Option.Some(Id("folder-New")))));
        fixture.Baseline.Has("Docs/New").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_new_folder_sits_inside_a_new_folder_then_the_parent_is_created_first_and_the_child_goes_inside_it()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.AddedFolder("Docs/A/B").AddedFolder("Docs/A").BuildAsync(CancellationToken);

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Folders.Calls.Select(call => (call.Name, call.Parent.Match(id => id.Value, () => "none"))).ShouldBe([("A", "remote-docs"), ("B", "folder-A")]);
    }

    [Fact]
    public async Task when_a_new_folder_already_exists_remotely_then_it_is_linked_and_nothing_is_created()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.Folder("Docs/Existing", "existing-id");
        _ = await fixture.AddedFolder("Docs/Existing").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Linked Docs/Existing"]);
        fixture.Folders.Calls.ShouldBeEmpty();
        fixture.Baseline.Find("Docs/Existing").ItemId.ShouldBe(Id("existing-id"));
    }

    [Fact]
    public async Task when_a_file_is_modified_and_the_remote_tag_is_unchanged_then_the_item_is_replaced_with_the_baseline_content_tag()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1");
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").ModifiedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Uploaded Docs/a.txt"]);
        var call = fixture.Uploader.Calls.ShouldHaveSingleItem();
        call.Target.ShouldBe(UploadTargetFactory.CreateReplacingItem(Id("r1")));
        call.Precondition.ShouldBe(UploadPreconditionFactory.CreateMustMatch(new ContentTag("c1")).Match(precondition => precondition, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public async Task when_a_file_is_modified_and_the_remote_changed_then_nothing_is_uploaded_and_it_goes_to_conflicts()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c2");
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").ModifiedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["SentToConflicts Docs/a.txt"]);
        fixture.Uploader.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_provider_answers_precondition_failed_for_a_replace_then_it_goes_to_conflicts_and_the_baseline_is_untouched()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").ModifiedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["SentToConflicts Docs/a.txt"]);
        result.Match(summary => summary.Outcomes.OfType<UploadItemOutcome.SentToConflicts>().Single().Kind, _ => throw new InvalidOperationException()).ShouldBe(ConflictKindFactory.CreateRemoteChangedDuringTransfer());
        fixture.Recorder.Recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_provider_answers_precondition_failed_for_a_new_file_then_it_goes_to_conflicts_as_remote_changed_during_transfer()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(summary => summary.Outcomes.OfType<UploadItemOutcome.SentToConflicts>().Single().Kind, _ => throw new InvalidOperationException()).ShouldBe(ConflictKindFactory.CreateRemoteChangedDuringTransfer());
    }

    [Fact]
    public async Task when_the_provider_answers_precondition_failed_and_the_classifier_has_no_answer_then_it_is_remote_changed_during_transfer()
    {
        fixture.Classifier = new SilentConflictClassifier();
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(summary => summary.Outcomes.OfType<UploadItemOutcome.SentToConflicts>().Single().Kind, _ => throw new InvalidOperationException()).ShouldBe(ConflictKindFactory.CreateRemoteChangedDuringTransfer());
    }

    [Fact]
    public async Task when_the_provider_answers_precondition_failed_then_the_kind_comes_from_the_classifier_with_a_precondition_signal()
    {
        var classifier = new RecordingConflictClassifier(ConflictKindFactory.CreateMoveTargetTaken());
        fixture.Classifier = classifier;
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(summary => summary.Outcomes.OfType<UploadItemOutcome.SentToConflicts>().Single().Kind, _ => throw new InvalidOperationException()).ShouldBe(ConflictKindFactory.CreateMoveTargetTaken());
        classifier.Signals.ShouldHaveSingleItem().ShouldBeOfType<ConflictSignal.PreconditionFailed>();
    }

    [Fact]
    public async Task when_the_provider_answers_name_conflict_for_a_new_file_then_it_goes_to_conflicts_as_both_added()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateNameConflict());
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(summary => summary.Outcomes.OfType<UploadItemOutcome.SentToConflicts>().Single().Kind, _ => throw new InvalidOperationException()).ShouldBe(ConflictKindFactory.CreateBothAdded());
    }

    [Fact]
    public async Task when_the_file_changes_during_the_upload_then_it_is_deferred_and_nothing_is_recorded()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateSourceChanged());
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Deferred Docs/a.txt"]);
        fixture.Recorder.Recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_one_upload_fails_with_a_transient_error_then_it_is_counted_as_failed_and_the_others_still_upload()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = name => name == "bad.txt" ? Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient()) : Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(Id("ok"), new ContentTag("c"), new EntityTag("e"), 1, Hash("h")));
        _ = await fixture.AddedFile("Docs/bad.txt").AddedFile("Docs/good.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Failed Docs/bad.txt", "Uploaded Docs/good.txt"]);
    }

    [Fact]
    public async Task when_recording_the_upload_fails_then_the_file_is_reported_as_failed_so_it_is_planned_again()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Recorder.Fails = true;
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Failed Docs/a.txt"]);
    }

    [Fact]
    public async Task when_the_provider_is_out_of_quota_then_later_uploads_are_not_attempted_and_the_pass_stops_with_quota_exceeded()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.MaxConcurrentUploads = 1;
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateQuotaExceeded());
        _ = await fixture.AddedFile("Docs/a.txt").AddedFile("Docs/b.txt").AddedFile("Docs/c.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["QuotaExceeded"]);
        fixture.Uploader.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_provider_is_out_of_quota_then_deletions_in_the_same_pass_still_run()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/old.txt", "r9", "c9");
        fixture.MaxConcurrentUploads = 1;
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateQuotaExceeded());
        _ = await fixture.AddedFile("Docs/a.txt").DeletedItem("Docs/old.txt", "r9").BaselineFile("Docs/old.txt", "r9", "c9").BuildAsync(CancellationToken);

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Remover.Calls.ShouldHaveSingleItem();
        fixture.Baseline.Has("Docs/old.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_account_must_sign_in_again_then_the_pass_stops_with_authentication_required()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateAuthenticationRequired());
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["AuthenticationRequired"]);
    }

    [Fact]
    public async Task when_a_name_is_rejected_then_it_is_reported_and_not_uploaded()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Names.Rejected.Add("bad.txt");
        _ = await fixture.AddedFile("Docs/bad.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Rejected Docs/bad.txt"]);
        fixture.Uploader.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_path_is_conflicted_then_the_pass_leaves_it_alone()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Quarantine = QuarantineSnapshotFactory.Create([new KeyValuePair<RelativeSyncPath, PathQuarantineState>(At("Docs/a.txt"), PathQuarantineStateFactory.CreateConflicted())]);
        _ = await fixture.AddedFile("Docs/a.txt").AddedFile("Docs/b.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["Uploaded Docs/b.txt"]);
    }

    [Fact]
    public async Task when_the_account_has_no_sync_root_then_the_pass_stops_with_root_unavailable()
    {
        fixture.HasRoot = false;
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RootUnavailable"]);
        fixture.Uploader.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_sync_root_folder_is_missing_then_the_pass_stops_with_root_unavailable()
    {
        _ = await fixture.AddedFile("Docs/a.txt").BuildAsync(CancellationToken);
        fixture.FileSystem.Directory.Delete("/sync");

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBe(["RootUnavailable"]);
    }

    [Fact]
    public async Task when_no_change_needs_uploading_then_nothing_is_called_and_the_summary_is_empty()
    {
        _ = await fixture.BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        Outcomes(result).ShouldBeEmpty();
        fixture.Uploader.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_many_files_are_uploaded_then_no_more_than_the_configured_number_run_at_once()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.MaxConcurrentUploads = 3;
        var gate = new WriteGate();
        fixture.Uploader.Gate = gate;
        foreach (var index in Enumerable.Range(0, 12))
            _ = fixture.AddedFile($"Docs/f{index}.txt");

        _ = await fixture.BuildAsync(CancellationToken);
        var running = fixture.RunAsync(CancellationToken);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        await Task.Delay(100, CancellationToken);
        gate.Release();
        var result = await running;

        fixture.Uploader.Peak.ShouldBe(3);
        result.Match(summary => summary.Uploaded, _ => -1).ShouldBe(12);
    }

    [Fact]
    public async Task when_the_pass_is_cancelled_then_no_new_upload_starts_and_the_rows_already_written_are_kept()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.MaxConcurrentUploads = 1;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Uploader.Respond = name =>
        {
            if (name == "a.txt")
                cancellation.Cancel();

            return Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(Id($"id-{name}"), new ContentTag("c"), new EntityTag("e"), 1, Hash("h")));
        };
        _ = await fixture.AddedFile("Docs/a.txt").AddedFile("Docs/b.txt").AddedFile("Docs/c.txt").BuildAsync(CancellationToken);

        var act = () => fixture.RunAsync(cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
        fixture.Uploader.Calls.Count.ShouldBe(1);
        fixture.Recorder.Recorded.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_pass_finishes_then_the_log_carries_counts_and_no_paths_or_names()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = await fixture.AddedFile("Docs/secret-name.txt").BuildAsync(CancellationToken);

        _ = await fixture.RunAsync(CancellationToken);

        var messages = string.Join('\n', fixture.Logs.Logger.Messages);
        messages.ShouldContain("1 uploaded");
        messages.ShouldNotContain("secret-name");
    }

    public void Dispose() => fixture.Dispose();
}
