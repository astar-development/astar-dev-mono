using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAHeldDeletionDecider : IDisposable
{
    private ResolutionFixture? fixture;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture?.Dispose();

    [Fact]
    public async Task when_cloud_deletions_are_confirmed_then_the_local_files_go_to_the_sync_trash_the_baseline_rows_go_and_the_set_is_closed()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a");
        run.Synced("Docs/b.txt", "two", "id-b");
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/a.txt", "Docs/b.txt");

        var result = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", error => error.Message), run.FilesUnder("Docs"), run.TrashedFiles().Count, run.Baseline.Has("Docs/a.txt"), run.Attention.Closed.Single()).ShouldBe(("ok", "", 2, false, id));
    }

    [Fact]
    public async Task when_a_confirmed_cloud_deletion_is_a_folder_then_the_whole_folder_goes_to_the_sync_trash()
    {
        var run = await StartAsync();
        run.SyncedFolder("Docs/old", "id-old");
        run.Local("Docs/old/inner.txt", "x");
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/old");

        _ = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        (run.FileSystem.Directory.Exists(ResolutionFixture.FullPath("Docs/old")), run.TrashedFiles().Count, run.Baseline.Has("Docs/old")).ShouldBe((false, 1, false));
    }

    [Fact]
    public async Task when_local_deletions_are_confirmed_then_the_remote_files_are_removed_with_an_if_match_on_the_baseline_ctag_and_the_set_is_closed()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a", "c-a");
        run.FileSystem.File.Delete(ResolutionFixture.FullPath("Docs/a.txt"));
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/a.txt");

        var result = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", error => error.Message), run.Remover.Calls.Single(), run.Baseline.Has("Docs/a.txt"), run.Attention.Closed.Single()).ShouldBe(("ok", (RemoteIds.Of("id-a"), ResolutionFixture.RemoveMustMatchContent("c-a")), false, id));
    }

    [Fact]
    public async Task when_a_local_folder_deletion_is_confirmed_then_the_remote_folder_is_removed_with_an_if_match_on_its_etag()
    {
        var run = await StartAsync();
        run.Remote.Folder("Docs/old", "id-old");
        run.SyncedFolder("Docs/old", "id-old");
        run.FileSystem.Directory.Delete(ResolutionFixture.FullPath("Docs/old"));
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/old");

        _ = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        run.Remover.Calls.Single().ItemId.ShouldBe(RemoteIds.Of("id-old"));
    }

    [Fact]
    public async Task when_a_remote_removal_is_refused_then_confirming_fails_and_the_set_stays_open()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a", "c-a");
        run.Remover.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/a.txt");

        var result = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", _ => "failed"), run.Attention.Closed.Count).ShouldBe(("failed", 0));
    }

    [Fact]
    public async Task when_local_deletions_are_restored_then_the_files_are_downloaded_again_and_the_set_is_closed()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a", "c-a");
        run.FileSystem.File.Delete(ResolutionFixture.FullPath("Docs/a.txt"));
        run.Remote.File("Docs/a.txt", "id-a", "c-a", 3);
        run.Downloader.Content["id-a"] = "one"u8.ToArray();
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/a.txt");

        var result = await run.Decider.RestoreAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", error => error.Message), run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/a.txt")), run.Attention.Closed.Single()).ShouldBe(("ok", "one", id));
    }

    [Fact]
    public async Task when_a_deleted_local_folder_is_restored_then_the_folder_is_created_before_its_files_are_downloaded()
    {
        var run = await StartAsync();
        run.Remote.Folder("Docs/old", "id-old");
        run.Remote.File("Docs/old/a.txt", "id-a", "c-a", 3);
        run.Downloader.Content["id-a"] = "one"u8.ToArray();
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/old/a.txt", "Docs/old");

        _ = await run.Decider.RestoreAsync(run.Account, id, CancellationToken);

        (run.FileSystem.File.ReadAllText(ResolutionFixture.FullPath("Docs/old/a.txt")), run.Attention.Closed.Count).ShouldBe(("one", 1));
    }

    [Fact]
    public async Task when_a_restore_download_fails_then_the_set_stays_open()
    {
        var run = await StartAsync();
        run.Remote.File("Docs/a.txt", "id-a", "c-a", 3);
        run.Downloader.Failures["id-a"] = ProviderErrorFactory.CreateTransient();
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/a.txt");

        var result = await run.Decider.RestoreAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", _ => "failed"), run.Attention.Closed.Count).ShouldBe(("failed", 0));
    }

    [Fact]
    public async Task when_cloud_deletions_are_restored_then_the_local_copies_are_uploaded_again_as_new_files_and_the_set_is_closed()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a");
        run.Remote.Folder("Docs");
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/a.txt");

        var result = await run.Decider.RestoreAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", error => error.Message), run.Uploader.Calls.Single().Precondition, run.Attention.Closed.Single()).ShouldBe(("ok", UploadPreconditionFactory.CreateMustNotExist(), id));
    }

    [Fact]
    public async Task when_a_restore_upload_fails_then_the_set_stays_open()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a");
        run.Remote.Folder("Docs");
        run.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient());
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/a.txt");

        var result = await run.Decider.RestoreAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", _ => "failed"), run.Attention.Closed.Count).ShouldBe(("failed", 0));
    }

    [Fact]
    public async Task when_the_set_is_not_in_the_queue_then_the_call_fails()
    {
        var run = await StartAsync();

        var result = await run.Decider.ConfirmAsync(run.Account, new HeldDeletionId(Guid.NewGuid()), CancellationToken);

        result.Match(_ => "ok", _ => "failed").ShouldBe("failed");
    }

    [Fact]
    public async Task when_closing_the_set_fails_then_confirming_fails()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a");
        run.Attention.CloseFails = true;
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/a.txt");

        var result = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        result.Match(_ => "ok", _ => "failed").ShouldBe("failed");
    }

    [Fact]
    public async Task when_a_sync_pass_is_running_then_confirming_is_refused_and_nothing_is_trashed()
    {
        var run = await StartAsync();
        run.Synced("Docs/a.txt", "one", "id-a");
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/a.txt");
        using var pass = ((SyncLockAttempt.Acquired)run.SyncLock.TryAcquire(run.Account)).Lease;

        var result = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        (result.Match(_ => "ok", _ => "refused"), run.TrashedFiles().Count).ShouldBe(("refused", 0));
    }

    [Fact]
    public async Task when_a_sync_pass_is_running_then_restoring_is_refused()
    {
        var run = await StartAsync();
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteRemote(), "Docs/a.txt");
        using var pass = ((SyncLockAttempt.Acquired)run.SyncLock.TryAcquire(run.Account)).Lease;

        var result = await run.Decider.RestoreAsync(run.Account, id, CancellationToken);

        result.Match(_ => "ok", _ => "refused").ShouldBe("refused");
    }

    [Fact]
    public async Task when_the_decider_runs_then_the_lease_is_released_afterwards()
    {
        var run = await StartAsync();
        var id = run.Hold(DeletionDirectionFactory.CreateDeleteLocal(), "Docs/a.txt");
        _ = await run.Decider.ConfirmAsync(run.Account, id, CancellationToken);

        var attempt = run.SyncLock.TryAcquire(run.Account);

        using var lease = attempt.ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        lease.AccountId.ShouldBe(run.Account);
    }

    private async Task<ResolutionFixture> StartAsync()
    {
        fixture = await ResolutionFixture.CreateAsync(CancellationToken);

        return fixture;
    }
}
