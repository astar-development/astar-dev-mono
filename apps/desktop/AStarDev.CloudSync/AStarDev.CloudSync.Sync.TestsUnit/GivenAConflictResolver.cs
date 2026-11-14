using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAConflictResolver : IDisposable
{
    private const string Stamp = "2026-11-14T090507Z";

    private ResolutionFixture? fixture;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture?.Dispose();

    [Fact]
    public async Task when_local_wins_for_both_modified_then_the_local_file_replaces_the_remote_with_an_if_match_on_the_current_ctag()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "old", "id-a", "c-old");
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.Uploader.Calls.Single().Precondition, run.Attention.ClosedConflicts.Single().Resolution).ShouldBe((1, ResolutionFixture.UploadMustMatch("c-current"), AttentionResolutionFactory.CreateKeepLocal()));
    }

    [Fact]
    public async Task when_local_wins_and_the_conflict_recorded_a_cloud_ctag_then_the_if_match_uses_the_ctag_the_user_saw()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-seen", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        run.Uploader.Calls.Single().Precondition.ShouldBe(ResolutionFixture.UploadMustMatch("c-seen"));
    }

    [Fact]
    public async Task when_the_cloud_file_changed_since_the_user_saw_it_then_local_wins_is_refused_as_remote_changed_before_any_upload()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-newer", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Uploader.Calls.Count, run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteChanged(), 0, 0));
    }

    [Fact]
    public async Task when_the_cloud_file_changed_since_the_user_saw_it_then_remote_wins_is_refused_and_the_local_file_is_kept()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-newer", 6);
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt")), run.Downloader.Calls.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteChanged(), "mine", 0));
    }

    [Fact]
    public async Task when_the_cloud_file_changed_since_the_user_saw_it_then_removing_it_is_refused()
    {
        var run = await StartAsync();
        run.Remote.File("Docs/a.txt", "id-a", "c-newer", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), local: SideSnapshotFactory.CreateAbsent(), seenCTag: "c-seen");

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Remover.Calls.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteChanged(), 0));
    }

    [Fact]
    public async Task when_the_upload_hits_a_412_then_the_conflict_stays_open_as_remote_changed()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        run.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, summary.Failed.Single().Reason, run.Attention.ClosedConflicts.Count).ShouldBe((0, ResolutionFailureFactory.CreateRemoteChanged(), 0));
    }

    [Fact]
    public async Task when_the_upload_fails_then_the_conflict_stays_open_with_the_provider_error()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        run.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient());
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateTransfer(ProviderErrorFactory.CreateTransient()), 0));
    }

    [Fact]
    public async Task when_the_local_file_is_gone_then_uploading_it_fails_as_local_file_missing()
    {
        var run = await StartAsync();
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Uploader.Calls.Count).ShouldBe((ResolutionFailureFactory.CreateLocalFileMissing(), 0));
    }

    [Fact]
    public async Task when_the_cloud_file_is_gone_then_replacing_it_fails_as_remote_file_missing()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Uploader.Calls.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteFileMissing(), 0));
    }

    [Fact]
    public async Task when_remote_wins_for_both_modified_then_the_remote_content_replaces_the_local_file_and_the_baseline_is_written()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "old", "id-a", "c-old");
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt")), run.Attention.ClosedConflicts.Single().Resolution, run.Baseline.Find("Docs/a.txt").Kind is SyncBaselineKind.File { CTag.Value: "c-downloaded" }).ShouldBe((1, "theirs", AttentionResolutionFactory.CreateKeepCloud(), true));
    }

    [Fact]
    public async Task when_the_download_fails_then_the_local_file_is_untouched_and_the_conflict_stays_open()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Failures["id-a"] = ProviderErrorFactory.CreateTransient();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt")), run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateTransfer(ProviderErrorFactory.CreateTransient()), "mine", 0));
    }

    [Fact]
    public async Task when_the_local_file_changes_during_the_download_then_it_is_not_overwritten_and_the_conflict_stays_open()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();
        run.Downloader.OnDownload = (_, _) =>
        {
            run.FileSystem.File.WriteAllText(ResolutionFixture.FullPath("Docs/a.txt"), "edited meanwhile");

            return Task.CompletedTask;
        };
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt")), run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateLocalChanged(), "edited meanwhile", 0));
    }

    [Fact]
    public async Task when_remote_wins_for_local_modified_remote_deleted_then_the_local_file_goes_to_the_sync_trash_and_is_not_deleted()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "old", "id-a");
        run.Local("Docs/a.txt", "precious");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), remote: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.FileSystem.File.Exists(ResolutionFixture.FullPath("Docs/a.txt")), string.Join(",", run.TrashedFiles().Select(run.FileSystem.File.ReadAllText)), run.Baseline.Has("Docs/a.txt")).ShouldBe((1, false, "precious", false));
    }

    [Fact]
    public async Task when_the_local_file_is_already_gone_then_remote_wins_for_local_modified_remote_deleted_still_closes_the_conflict()
    {
        var run = await StartAsync();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), remote: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.TrashedFiles().Count).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_local_wins_for_local_modified_remote_deleted_then_the_file_is_uploaded_as_a_new_file()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.Folder("Docs");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), remote: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.Uploader.Calls.Single().Precondition, run.Uploader.Calls.Single().Target is UploadTarget.NewFileIn).ShouldBe((1, UploadPreconditionFactory.CreateMustNotExist(), true));
    }

    [Fact]
    public async Task when_keep_both_for_local_modified_remote_deleted_then_the_file_is_uploaded_as_a_new_file_and_closed_as_keep_both()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.Folder("Docs");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), remote: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (summary.Resolved, run.Attention.ClosedConflicts.Single().Resolution).ShouldBe((1, AttentionResolutionFactory.CreateKeepBoth()));
    }

    [Fact]
    public async Task when_local_wins_for_local_deleted_remote_modified_then_the_remote_is_removed_with_an_if_match_on_the_current_ctag_and_the_baseline_row_goes()
    {
        var run = await StartAsync();
        _ = run.Synced("Docs/a.txt", "old", "id-a", "c-old");
        run.FileSystem.File.Delete(ResolutionFixture.FullPath("Docs/a.txt"));
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), local: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.Remover.Calls.Single(), run.Baseline.Has("Docs/a.txt")).ShouldBe((1, (RemoteIds.Of("id-a"), ResolutionFixture.RemoveMustMatchContent("c-current")), false));
    }

    [Fact]
    public async Task when_the_remote_removal_is_refused_then_the_conflict_stays_open_as_remote_changed()
    {
        var run = await StartAsync();
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        run.Remover.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), local: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteChanged(), 0));
    }

    [Fact]
    public async Task when_remote_wins_for_local_deleted_remote_modified_then_the_file_is_downloaded_again()
    {
        var run = await StartAsync();
        _ = run.FileSystem.Directory.CreateDirectory(ResolutionFixture.FullPath("Docs"));
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), local: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt"))).ShouldBe((1, "theirs"));
    }

    [Fact]
    public async Task when_keep_both_for_both_modified_then_the_local_copy_gets_a_utc_name_is_uploaded_and_the_remote_goes_to_the_original_name()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (summary.Resolved, run.FilesUnder("Docs"), run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath($"Docs/a-({Stamp}).txt")), run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt")), run.Uploader.Calls.Single().Name, run.Attention.ClosedConflicts.Single().Resolution)
            .ShouldBe((1, "a-(" + Stamp + ").txt,a.txt", "mine", "theirs", $"a-({Stamp}).txt", AttentionResolutionFactory.CreateKeepBoth()));
    }

    [Fact]
    public async Task when_the_keep_both_name_is_taken_then_a_counter_is_added()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Local($"Docs/a-({Stamp}).txt", "other");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath($"Docs/a-({Stamp})-2.txt")).ShouldBe("mine");
    }

    [Fact]
    public async Task when_the_keep_both_upload_fails_then_the_local_copy_is_renamed_back_and_the_conflict_stays_open()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient());
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (summary.Failed.Count, run.FilesUnder("Docs"), run.Downloader.Calls.Count, run.Attention.ClosedConflicts.Count).ShouldBe((1, "a.txt", 0, 0));
    }

    [Fact]
    public async Task when_the_keep_both_download_fails_then_the_conflict_stays_open_and_a_retry_does_not_rename_again()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 6);
        run.Downloader.Failures["id-a"] = ProviderErrorFactory.CreateTransient();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));
        run.Downloader.Failures.Clear();
        run.Downloader.Content["id-a"] = "theirs"u8.ToArray();

        var retry = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (retry.Resolved, run.FilesUnder("Docs"), run.Uploader.Calls.Count).ShouldBe((1, "a-(" + Stamp + ").txt,a.txt", 1));
    }

    [Fact]
    public async Task when_skip_is_chosen_then_nothing_changes_the_item_stays_open_and_a_new_service_provider_still_lists_it()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateSkip(), CancellationToken));

        var open = (await run.Attention.GetOpenAsync(run.Account, CancellationToken)).Match(items => items.Count, _ => -1);
        (summary.Skipped, summary.Resolved, open, run.Uploader.Calls.Count, run.Downloader.Calls.Count).ShouldBe((1, 0, 1, 0, 0));
    }

    [Fact]
    public async Task when_two_conflicts_share_a_path_ignoring_case_then_one_strategy_settles_both_with_one_transfer()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var first = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        var second = run.Conflict("DOCS/A.TXT", ConflictKindFactory.CreateBothAdded());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [first], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.Uploader.Calls.Count, run.Attention.ClosedConflicts.Select(closed => closed.Id).Count(closed => closed == first || closed == second)).ShouldBe((2, 1, 2));
    }

    [Fact]
    public async Task when_the_same_id_is_listed_twice_then_it_is_resolved_once()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id, id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.Uploader.Calls.Count).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_only_some_conflicts_are_selected_then_the_others_are_untouched()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Local("Docs/b.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        run.Remote.File("Docs/b.txt", "id-b", "c-current", 4);
        var selected = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        _ = run.Conflict("Docs/b.txt", ConflictKindFactory.CreateBothModified());

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [selected], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (string.Join(",", run.Uploader.Calls.Select(call => call.Name)), run.Attention.ClosedConflicts.Count).ShouldBe(("a.txt", 1));
    }

    [Fact]
    public async Task when_the_kind_is_move_target_taken_then_it_stays_open_as_unsupported()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateMoveTargetTaken());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateUnsupported(), 0));
    }

    [Fact]
    public async Task when_the_strategy_does_not_apply_then_the_item_fails_as_not_allowed_with_the_reason()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified(), remote: SideSnapshotFactory.CreateAbsent());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        summary.Failed.Single().Reason.ShouldBe(ResolutionFailureFactory.CreateNotAllowed(ResolutionNotAllowedFactory.CreateRemoteSideAbsent()));
    }

    [Fact]
    public async Task when_an_id_is_not_in_the_queue_then_it_fails_as_not_open()
    {
        var run = await StartAsync();
        var unknown = new ConflictId(Guid.NewGuid());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [unknown], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        summary.Failed.Single().ShouldBe(FailedResolutionFactory.Create(unknown, ResolutionFailureFactory.CreateNotOpen()));
    }

    [Fact]
    public async Task when_closing_the_item_fails_after_the_transfer_then_it_fails_as_not_recorded()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        run.Attention.CloseFails = true;
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, summary.Failed.Single().Reason).ShouldBe((0, ResolutionFailureFactory.CreateNotRecorded()));
    }

    [Fact]
    public async Task when_the_queue_cannot_be_read_then_the_call_fails_with_an_error()
    {
        var run = await StartAsync();
        run.Attention.ReadFails = true;

        var result = await run.Resolver.ResolveAsync(run.Account, [new ConflictId(Guid.NewGuid())], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        result.Match(_ => "ok", error => error.Message).ShouldNotBe("ok");
    }

    [Fact]
    public async Task when_a_sync_pass_is_running_then_the_call_is_refused_without_touching_anything()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        using var pass = ((SyncLockAttempt.Acquired)run.SyncLock.TryAcquire(run.Account)).Lease;

        var result = await run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        (result.Match(_ => "ok", _ => "refused"), run.Uploader.Calls.Count, run.Attention.ClosedConflicts.Count).ShouldBe(("refused", 0, 0));
    }

    [Fact]
    public async Task when_the_lock_is_draining_then_the_call_is_refused()
    {
        var run = await StartAsync();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        using var drain = await run.SyncLock.DrainAsync(CancellationToken);

        var result = await run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        result.Match(_ => "ok", error => error.Message).ShouldNotBe("ok");
    }

    [Fact]
    public async Task when_the_call_ends_then_the_account_lease_is_released_so_a_pass_can_start()
    {
        var run = await StartAsync();
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        _ = await run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateSkip(), CancellationToken);

        var attempt = run.SyncLock.TryAcquire(run.Account);

        using var lease = attempt.ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        lease.AccountId.ShouldBe(run.Account);
    }

    [Fact]
    public async Task when_a_resolution_is_running_then_a_sync_pass_cannot_start_for_the_account()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var gate = new WriteGate();
        run.Uploader.Gate = gate;
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        var resolving = Task.Run(() => run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken), CancellationToken);
        await gate.Entered.WaitAsync(CancellationToken);

        var duringResolution = run.SyncLock.TryAcquire(run.Account);

        gate.Release();
        _ = await resolving;
        duringResolution.ShouldBeOfType<SyncLockAttempt.Busy>();
    }

    [Fact]
    public async Task when_a_sync_pass_holds_the_lease_then_a_resolution_never_starts_its_transfer()
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        using var pass = ((SyncLockAttempt.Acquired)run.SyncLock.TryAcquire(run.Account)).Lease;

        _ = await run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        run.Uploader.Calls.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task when_twenty_resolutions_and_passes_race_then_they_never_overlap_and_exactly_one_resolution_wins(int round)
    {
        var run = await StartAsync();
        run.Local("Docs/a.txt", "mine");
        run.Remote.File("Docs/a.txt", "id-a", "c-current", 4);
        var gate = new WriteGate();
        run.Uploader.Gate = gate;
        var id = run.Conflict("Docs/a.txt", ConflictKindFactory.CreateBothModified());
        var passesStarted = 0;
        var resolutions = Enumerable.Range(0, 20).Select(_ => Task.Run(() => run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken), CancellationToken)).ToList();
        await gate.Entered.WaitAsync(CancellationToken);
        var passes = Enumerable.Range(0, 20).Select(_ => Task.Run(() => run.SyncLock.TryAcquire(run.Account) is SyncLockAttempt.Acquired acquired ? Interlocked.Increment(ref passesStarted) + Release(acquired) : 0, CancellationToken)).ToList();
        _ = await Task.WhenAll(passes);
        while (resolutions.Count(resolution => resolution.IsCompleted) < 19)
            await Task.Delay(5, CancellationToken);

        gate.Release();

        var results = await Task.WhenAll(resolutions);

        (passesStarted, run.Uploader.Peak, results.Count(result => result.Match(_ => true, _ => false)), run.Uploader.Calls.Count).ShouldBe((0, 1, 1, 1), $"round {round}");
    }

    [Fact]
    public async Task when_a_resolution_runs_then_the_logs_carry_no_paths_names_or_account_data()
    {
        var run = await StartAsync();
        run.Local("Docs/secret-plan.txt", "mine");
        run.Remote.File("Docs/secret-plan.txt", "id-a", "c-current", 4);
        var id = run.Conflict("Docs/secret-plan.txt", ConflictKindFactory.CreateBothModified());

        _ = await run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        run.Logs.Logger.Messages.Where(message => message.Contains("secret-plan", StringComparison.OrdinalIgnoreCase) || message.Contains("Docs", StringComparison.Ordinal) || message.Contains("jason", StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();
    }

    private static int Release(SyncLockAttempt.Acquired acquired)
    {
        acquired.Lease.Dispose();

        return 0;
    }

    private static async Task<ResolutionSummary> Summary(Task<Result<ResolutionSummary, ErrorResponse>> call) =>
        (await call).Match(summary => summary, error => throw new InvalidOperationException($"The call failed: {error.Message}"));

    private async Task<ResolutionFixture> StartAsync()
    {
        fixture = await ResolutionFixture.CreateAsync(CancellationToken);

        return fixture;
    }
}
