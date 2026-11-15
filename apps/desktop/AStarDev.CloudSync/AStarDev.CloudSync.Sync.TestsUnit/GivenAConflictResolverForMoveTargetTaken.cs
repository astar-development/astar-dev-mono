using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAConflictResolverForMoveTargetTaken : IDisposable
{
    private const string Stamp = "2026-11-14T090507Z";
    private const string Target = "Docs/new.txt";
    private const string CopyName = "new-(" + Stamp + ").txt";

    private ResolutionFixture? fixture;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture?.Dispose();

    [Fact]
    public async Task when_local_wins_then_the_remote_item_is_renamed_to_a_keep_both_name_and_the_local_file_stays()
    {
        var (run, id) = await StartAsync();

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.Renamer.Calls.Single().ItemId, run.Renamer.Calls.Single().NewName, run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath(Target)), run.Attention.ClosedConflicts.Single().Resolution)
            .ShouldBe((1, RemoteIds.Of("id-m"), CopyName, "mine", AttentionResolutionFactory.CreateKeepLocal()));
    }

    [Fact]
    public async Task when_local_wins_and_the_name_is_taken_remotely_then_the_conflict_stays_open_with_the_provider_error()
    {
        var (run, id) = await StartAsync();
        run.Renamer.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateNameConflict());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateTransfer(ProviderErrorFactory.CreateNameConflict()), 0));
    }

    [Fact]
    public async Task when_local_wins_and_the_moved_item_changed_since_the_user_saw_it_then_nothing_is_renamed()
    {
        var (run, id) = await StartAsync(seenCTag: "c-older");

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Renamer.Calls.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteChanged(), 0));
    }

    [Fact]
    public async Task when_local_wins_and_the_remote_item_is_gone_then_the_conflict_stays_open()
    {
        var run = await ResolutionFixture.CreateAsync(CancellationToken);
        fixture = run;
        run.Local(Target, "mine");
        var id = run.Conflict(Target, ConflictKindFactory.CreateMoveTargetTaken(), seenCTag: "c-m");

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Renamer.Calls.Count).ShouldBe((ResolutionFailureFactory.CreateRemoteFileMissing(), 0));
    }

    [Fact]
    public async Task when_local_wins_and_another_item_was_synced_at_the_target_then_its_baseline_row_is_dropped_so_the_local_file_uploads_as_new()
    {
        var (run, id) = await StartAsync();
        _ = run.Synced(Target, "older", "id-b");

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        run.Baseline.Has(Target).ShouldBeFalse();
    }

    [Fact]
    public async Task when_local_wins_and_the_baseline_row_cannot_be_dropped_then_the_remote_is_not_renamed_so_a_retry_cannot_rename_twice()
    {
        var (run, id) = await StartAsync();
        _ = run.Synced(Target, "older", "id-b");
        run.Baseline.RemoveFails = true;

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.Renamer.Calls.Count, run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateNotRecorded(), 0, 0));
    }

    [Fact]
    public async Task when_local_wins_then_the_moved_item_is_settled_at_its_keep_both_name_and_its_baseline_row_follows()
    {
        var (run, id) = await StartAsync();

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/" + CopyName)), run.FileSystem.File.Exists(ResolutionFixture.FullPath("Docs/old.txt")), run.Baseline.Has("Docs/" + CopyName), run.Baseline.Has("Docs/old.txt"))
            .ShouldBe(("moved", false, true, false));
    }

    [Fact]
    public async Task when_local_wins_then_the_index_row_is_renamed_to_the_keep_both_name()
    {
        var (run, id) = await StartAsync();

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        run.IndexWriter.Renames.ShouldBe([(RemoteIds.Of("id-m"), CopyName)]);
    }

    [Fact]
    public async Task when_local_wins_then_the_moved_items_baseline_row_is_found_by_its_id_without_paging_the_baseline()
    {
        var (run, id) = await StartAsync();
        for (var index = 0; index < 1200; index++)
            _ = run.Synced($"Docs/filler-{index:D4}.txt", "x", $"filler-{index}");
        var pagesBefore = run.Baseline.Pages;

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (run.Baseline.Pages - pagesBefore, run.Baseline.ItemLookups, run.Baseline.Has("Docs/" + CopyName)).ShouldBe((0, 1, true));
    }

    [Fact]
    public async Task when_local_wins_and_the_index_cannot_be_renamed_then_the_conflict_is_still_resolved_and_the_next_pass_follows_the_move()
    {
        var (run, id) = await StartAsync();
        run.IndexWriter.RenameFails = true;

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.FileSystem.File.Exists(ResolutionFixture.FullPath("Docs/old.txt")), run.Baseline.Has("Docs/old.txt"), run.Attention.ClosedConflicts.Count).ShouldBe((1, true, true, 1));
    }

    [Fact]
    public async Task when_local_wins_and_the_old_local_file_is_gone_then_the_conflict_is_resolved_and_nothing_moves()
    {
        var (run, id) = await StartAsync();
        run.FileSystem.File.Delete(ResolutionFixture.FullPath("Docs/old.txt"));

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.FileSystem.File.Exists(ResolutionFixture.FullPath("Docs/" + CopyName)), run.Baseline.Has("Docs/" + CopyName)).ShouldBe((1, false, false));
    }

    [Fact]
    public async Task when_local_wins_and_the_baseline_cannot_be_recorded_then_the_conflict_is_resolved_and_the_old_file_stays_where_it_was()
    {
        var (run, id) = await StartAsync();
        run.Baseline.UpsertFails = true;

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        (summary.Resolved, run.FileSystem.File.Exists(ResolutionFixture.FullPath("Docs/old.txt")), run.FileSystem.File.Exists(ResolutionFixture.FullPath("Docs/" + CopyName))).ShouldBe((1, true, false));
    }

    [Fact]
    public async Task when_remote_wins_then_the_local_file_is_renamed_to_a_keep_both_name_and_the_remote_is_untouched()
    {
        var (run, id) = await StartAsync();

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.FilesUnder("Docs"), run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/" + CopyName)), run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath(Target)), run.Renamer.Calls.Count, run.Uploader.Calls.Count, run.Attention.ClosedConflicts.Single().Resolution)
            .ShouldBe((1, $"{CopyName},new.txt", "mine", "moved", 0, 0, AttentionResolutionFactory.CreateKeepCloud()));
    }

    [Fact]
    public async Task when_remote_wins_and_another_item_was_synced_at_the_target_then_its_baseline_row_is_replaced_by_the_moved_item()
    {
        var (run, id) = await StartAsync();
        _ = run.Synced(Target, "older", "id-b");

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        run.Baseline.Find(Target).ItemId.ShouldBe(RemoteIds.Of("id-m"));
    }

    [Fact]
    public async Task when_remote_wins_then_the_moved_item_is_settled_at_the_target_without_a_sync_pass()
    {
        var (run, id) = await StartAsync();

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (run.Baseline.Has("Docs/old.txt"), run.Baseline.Find(Target).ItemId, run.Baseline.Find(Target).Kind.ShouldBeOfType<SyncBaselineKind.File>().CTag).ShouldBe((false, RemoteIds.Of("id-m"), RemoteIds.CTag("c-m")));
    }

    [Fact]
    public async Task when_remote_wins_and_the_baseline_cannot_be_recorded_then_the_local_move_is_undone_and_the_next_pass_still_follows_the_move()
    {
        var (run, id) = await StartAsync();
        run.Baseline.UpsertFails = true;

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.FilesUnder("Docs"), run.Baseline.Has("Docs/old.txt"), run.Baseline.Has(Target)).ShouldBe((1, $"{CopyName},old.txt", true, false));
    }

    [Fact]
    public async Task when_remote_wins_and_the_old_local_file_is_gone_then_the_conflict_still_resolves_and_the_baseline_is_left_for_the_next_pass()
    {
        var (run, id) = await StartAsync();
        run.FileSystem.File.Delete(ResolutionFixture.FullPath("Docs/old.txt"));

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Resolved, run.FilesUnder("Docs"), run.Baseline.Has("Docs/old.txt"), run.Baseline.Has(Target)).ShouldBe((1, CopyName, true, false));
    }

    [Fact]
    public async Task when_remote_wins_and_the_target_is_a_folder_then_nothing_moves_and_the_conflict_stays_open_as_unsupported()
    {
        var run = await ResolutionFixture.CreateAsync(CancellationToken);
        fixture = run;
        _ = run.FileSystem.Directory.CreateDirectory(ResolutionFixture.FullPath(Target));
        run.Remote.File(Target, "id-m", "c-m", 4);
        var id = run.Conflict(Target, ConflictKindFactory.CreateMoveTargetTaken(), seenCTag: "c-m");

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken));

        (summary.Failed.Single().Reason, run.FileSystem.Directory.Exists(ResolutionFixture.FullPath(Target)), run.Attention.ClosedConflicts.Count).ShouldBe((ResolutionFailureFactory.CreateUnsupported(), true, 0));
    }

    [Fact]
    public async Task when_keep_both_then_the_local_file_is_renamed_and_uploaded_as_a_new_file_and_the_remote_is_untouched()
    {
        var (run, id) = await StartAsync();

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (summary.Resolved, run.FilesUnder("Docs"), run.Uploader.Calls.Single().Name, run.Renamer.Calls.Count, run.Attention.ClosedConflicts.Single().Resolution)
            .ShouldBe((1, $"{CopyName},new.txt", CopyName, 0, AttentionResolutionFactory.CreateKeepBoth()));
    }

    [Fact]
    public async Task when_keep_both_then_the_moved_item_is_settled_at_the_target_without_a_sync_pass()
    {
        var (run, id) = await StartAsync();

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath(Target)), run.Baseline.Has("Docs/old.txt"), run.Baseline.Find(Target).ItemId).ShouldBe(("moved", false, RemoteIds.Of("id-m")));
    }

    [Fact]
    public async Task when_local_wins_and_the_keep_both_name_is_taken_by_another_cloud_item_then_the_remote_is_renamed_to_a_free_name()
    {
        var (run, id) = await StartAsync();
        run.Remote.File("Docs/" + CopyName, "id-other", "c-other", 3);

        _ = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken));

        run.Renamer.Calls.Single().NewName.ShouldNotBeOneOf(CopyName, "new.txt");
    }

    [Fact]
    public async Task when_keep_both_and_the_upload_fails_then_the_local_file_goes_back_to_its_name_and_the_conflict_stays_open()
    {
        var (run, id) = await StartAsync();
        run.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient());

        var summary = await Summary(run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken));

        (summary.Failed.Count, run.FilesUnder("Docs"), run.Attention.ClosedConflicts.Count).ShouldBe((1, "new.txt,old.txt", 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task when_twenty_resolutions_race_for_one_move_target_conflict_then_the_remote_is_renamed_exactly_once(int round)
    {
        var (run, id) = await StartAsync();
        var gate = new WriteGate();
        run.Renamer.Gate = gate;
        var resolutions = Enumerable.Range(0, 20).Select(_ => Task.Run(() => run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken), CancellationToken)).ToList();
        var entered = await Task.WhenAny(gate.Entered, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken));
        if (entered == gate.Entered)
            while (resolutions.Count(resolution => resolution.IsCompleted) < 19)
                await Task.Delay(5, CancellationToken);

        gate.Release();

        var results = await Task.WhenAll(resolutions);

        (run.Renamer.Calls.Count, results.Count(result => result.Match(_ => true, _ => false)), run.Attention.ClosedConflicts.Count).ShouldBe((1, 1, 1), $"round {round}");
    }

    [Fact]
    public async Task when_a_move_target_resolution_runs_then_the_logs_carry_no_paths_or_names()
    {
        var (run, id) = await StartAsync();

        _ = await run.Resolver.ResolveAsync(run.Account, [id], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        run.Logs.Logger.Messages.Where(message => message.Contains("new", StringComparison.OrdinalIgnoreCase) || message.Contains("old", StringComparison.OrdinalIgnoreCase) || message.Contains("Docs", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    private static async Task<ResolutionSummary> Summary(Task<Result<ResolutionSummary, ConflictRefusal>> call) =>
        (await call).Match(summary => summary, refusal => throw new InvalidOperationException($"The call was refused: {refusal.GetType().Name}"));

    private async Task<(ResolutionFixture Run, ConflictId Id)> StartAsync(string seenCTag = "c-m")
    {
        var run = await ResolutionFixture.CreateAsync(CancellationToken);
        fixture = run;
        _ = run.Synced("Docs/old.txt", "moved", "id-m", "c-m");
        run.Local(Target, "mine");
        run.Remote.File(Target, "id-m", "c-m", 4);

        return (run, run.Conflict(Target, ConflictKindFactory.CreateMoveTargetTaken(), seenCTag: seenCTag));
    }
}
