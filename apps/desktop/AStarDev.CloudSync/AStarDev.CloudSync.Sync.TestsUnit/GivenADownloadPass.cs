using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPass
{
    private static readonly byte[] Hello = "hello"u8.ToArray();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_new_remote_file_exists_then_it_is_downloaded_with_the_remote_write_time_and_a_baseline_row()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Downloaded.ShouldBe(1);
        summary.Bytes.ShouldBe(5);
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/a.txt").ShouldBe("hello");
        fixture.FileSystem.File.GetLastWriteTimeUtc("/sync/root/Docs/a.txt").ShouldBe(new DateTime(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc));
        var row = fixture.Baseline.Find("Docs/a.txt");
        (row.Kind as SyncBaselineKind.File)!.Size.ShouldBe(5);
        (row.Kind as SyncBaselineKind.File)!.LastWriteUtc.UtcDateTime.ShouldBe(new DateTime(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc));
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").Length.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_second_pass_finds_no_remote_change_then_nothing_is_downloaded_again()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c-downloaded", 5);
        fixture.Downloader.Content["a"] = Hello;
        _ = await fixture.RunAsync(CancellationToken);

        var second = Summary(await fixture.RunAsync(CancellationToken));

        second.HasChanges().ShouldBeFalse();
        fixture.Downloader.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_remote_folder_is_new_then_it_is_created_and_recorded()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs/Sub");

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.FoldersCreated.ShouldBe(1);
        fixture.FileSystem.Directory.Exists("/sync/root/Docs/Sub").ShouldBeTrue();
        fixture.Baseline.Has("Docs/Sub").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_remote_file_was_deleted_then_the_local_file_and_its_baseline_row_are_removed_and_the_pass_reports_it()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a");
        fixture.Remote.Folder("Docs");

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.DeletedLocally.ShouldBe(1);
        summary.HasChanges().ShouldBeTrue();
        fixture.FileSystem.File.Exists("/sync/root/Docs/a.txt").ShouldBeFalse();
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_remote_file_was_renamed_then_it_is_moved_without_a_download_and_the_baseline_follows()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a");
        fixture.Remote.File("Docs/b.txt", "a", "c1", 5);

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Moved.ShouldBe(1);
        fixture.Downloader.Calls.ShouldBeEmpty();
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/b.txt").ShouldBe("hello");
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
        fixture.Baseline.Has("Docs/b.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_remote_folder_was_renamed_then_the_folder_moves_once_and_every_baseline_row_below_follows()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.SyncedFolder("Docs/Old", "folder");
        _ = fixture.Synced("Docs/Old/a.txt", "hello", "a");
        _ = fixture.Synced("Docs/Old/b.txt", "world", "b");
        fixture.Remote.Folder("Docs/New", "folder");
        fixture.Remote.File("Docs/New/a.txt", "a", "c1", 5);
        fixture.Remote.File("Docs/New/b.txt", "b", "c1", 5);

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Moved.ShouldBe(1);
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/New/a.txt").ShouldBe("hello");
        fixture.Baseline.All.Select(entry => entry.Path.Value).Order().ShouldBe(["Docs", "Docs/New", "Docs/New/a.txt", "Docs/New/b.txt"]);
    }

    [Fact]
    public async Task when_a_pair_loses_more_than_a_quarter_and_at_least_fifty_items_then_nothing_is_deleted_and_the_summary_reports_the_held_deletions()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        for (var index = 0; index < 100; index++)
        {
            _ = fixture.Synced($"Docs/f{index:000}.txt", "x", $"id-{index}");
            if (index >= 60)
                fixture.Remote.File($"Docs/f{index:000}.txt", $"id-{index}", "c1", 1);
        }

        fixture.Remote.File("Docs/new.txt", "new", "c2", 5);
        fixture.Downloader.Content["new"] = Hello;

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.DeletedLocally.ShouldBe(0);
        summary.HeldByGuard.ShouldBe(60);
        summary.Downloaded.ShouldBe(1);
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").Length.ShouldBe(101);
        summary.Held.ShouldHaveSingleItem().Paths.Count.ShouldBe(60);
        fixture.Attention.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_local_file_changes_between_the_plan_and_the_move_then_it_is_left_alone_and_sent_to_conflicts()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a");
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = "remote"u8.ToArray()[..5];
        fixture.Downloader.OnDownload = (_, _) =>
        {
            fixture.FileSystem.File.WriteAllText("/sync/root/Docs/a.txt", "my edit");

            return Task.CompletedTask;
        };

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.SentToConflicts.ShouldBe(1);
        summary.Downloaded.ShouldBe(0);
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/a.txt").ShouldBe("my edit");
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").Length.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_new_file_appears_locally_during_the_download_then_it_is_left_alone_and_sent_to_conflicts()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Downloader.OnDownload = (_, _) =>
        {
            _ = fixture.Write("Docs/a.txt", "new local");

            return Task.CompletedTask;
        };

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.SentToConflicts.ShouldBe(1);
        summary.Conflicts.ShouldHaveSingleItem().Kind.ShouldBe(ConflictKindFactory.CreateRemoteChangedDuringTransfer());
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/a.txt").ShouldBe("new local");
    }

    [Fact]
    public async Task when_the_target_changes_during_the_download_and_the_classifier_has_no_answer_then_it_is_remote_changed_during_transfer()
    {
        using var fixture = await CreateAsync(CancellationToken, classifier: new SilentConflictClassifier());
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Downloader.OnDownload = (_, _) =>
        {
            _ = fixture.Write("Docs/a.txt", "new local");

            return Task.CompletedTask;
        };

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Conflicts.ShouldHaveSingleItem().Kind.ShouldBe(ConflictKindFactory.CreateRemoteChangedDuringTransfer());
    }

    [Fact]
    public async Task when_the_target_changes_during_the_download_then_the_kind_comes_from_the_classifier_with_a_precondition_signal()
    {
        var classifier = new RecordingConflictClassifier(ConflictKindFactory.CreateMoveTargetTaken());
        using var fixture = await CreateAsync(CancellationToken, classifier: classifier);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Downloader.OnDownload = (_, _) =>
        {
            _ = fixture.Write("Docs/a.txt", "new local");

            return Task.CompletedTask;
        };

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Conflicts.ShouldHaveSingleItem().Kind.ShouldBe(ConflictKindFactory.CreateMoveTargetTaken());
        classifier.Signals.ShouldHaveSingleItem().ShouldBeOfType<ConflictSignal.PreconditionFailed>();
    }

    [Fact]
    public async Task when_the_downloaded_size_differs_from_the_remote_size_then_the_file_fails_and_no_temp_file_is_left()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Downloader.Truncate["a"] = 3;

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Failed.ShouldBe(1);
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").ShouldBeEmpty();
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_remote_file_is_gone_then_it_is_skipped_and_the_other_files_still_download()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Remote.File("Docs/b.txt", "b", "c2", 5);
        fixture.Downloader.Content["b"] = Hello;
        fixture.Downloader.Failures["a"] = ProviderErrorFactory.CreateNotFound();

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        (summary.Skipped, summary.Downloaded, summary.Failed).ShouldBe((1, 1, 0));
    }

    [Fact]
    public async Task when_the_provider_throttles_one_file_then_it_fails_and_the_next_pass_can_retry()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Failures["a"] = ProviderErrorFactory.CreateThrottled(Option.None<TimeSpan>());

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Failed.ShouldBe(1);
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_disk_is_too_full_then_the_pass_stops_with_insufficient_disk_space_and_keeps_the_rows_of_the_files_already_downloaded()
    {
        using var fixture = await CreateAsync(CancellationToken, DownloadPassOptionsFactory.Create(1, 100, 1));
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Remote.File("Docs/b.txt", "b", "c2", 5);
        fixture.Remote.File("Docs/c.txt", "c", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Disk.Free = Option.Some(1000L);
        fixture.Downloader.OnDownload = (_, _) =>
        {
            fixture.Disk.Free = Option.Some(50L);

            return Task.CompletedTask;
        };

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.InsufficientDiskSpace).ShouldBeTrue();
        fixture.Baseline.Has("Docs/a.txt").ShouldBeTrue();
        fixture.Downloader.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_free_space_is_unknown_then_the_download_goes_ahead()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Disk.Free = Option.None<long>();

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Downloaded.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_pass_starts_then_orphaned_temp_files_are_deleted_and_other_files_are_kept()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs");
        _ = fixture.Write("Docs/a.txt.0f8fad5bd9cb469fa16570867728950e.download", "partial");
        _ = fixture.Write("Docs/notes.download", "mine");

        _ = await fixture.RunAsync(CancellationToken);

        fixture.FileSystem.File.Exists("/sync/root/Docs/a.txt.0f8fad5bd9cb469fa16570867728950e.download").ShouldBeFalse();
        fixture.FileSystem.File.Exists("/sync/root/Docs/notes.download").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_pass_is_cancelled_during_a_download_then_the_temp_file_is_deleted_and_the_finished_rows_are_kept()
    {
        using var fixture = await CreateAsync(CancellationToken, DownloadPassOptionsFactory.Create(1, 100, 1));
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Remote.File("Docs/b.txt", "b", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;
        fixture.Downloader.Content["b"] = Hello;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Downloader.OnDownload = (id, _) =>
        {
            if (id == "b")
                cancellation.Cancel();

            return Task.CompletedTask;
        };

        var running = fixture.RunAsync(cancellation.Token);
        var cancelled = await Should.ThrowAsync<OperationCanceledException>(() => running);

        cancelled.ShouldNotBeNull();
        fixture.Baseline.Has("Docs/a.txt").ShouldBeTrue();
        fixture.Baseline.Has("Docs/b.txt").ShouldBeFalse();
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").Length.ShouldBe(1);
    }

    [Fact]
    public async Task when_many_files_download_then_at_most_the_configured_number_run_at_once()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Downloader.Delay = TimeSpan.FromMilliseconds(20);
        for (var index = 0; index < 24; index++)
        {
            fixture.Remote.File($"Docs/f{index}.txt", $"id-{index}", "c2", 5);
            fixture.Downloader.Content[$"id-{index}"] = Hello;
        }

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Downloaded.ShouldBe(24);
        fixture.Downloader.MostRunning.ShouldBeInRange(2, 8);
    }

    [Fact]
    public async Task when_the_account_has_no_sync_root_then_the_pass_reports_root_unavailable()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Roots.Root = Option.None<System.IO.Abstractions.IDirectoryInfo>();

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.RootUnavailable).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_sync_root_folder_is_missing_then_the_pass_reports_root_unavailable_and_changes_nothing()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.FileSystem.Directory.Delete("/sync/root", recursive: true);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.RootUnavailable).ShouldBeTrue();
        fixture.FileSystem.Directory.Exists("/sync/root").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_account_sync_root_cannot_be_read_then_the_pass_reports_root_unavailable()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Roots.Failure = Result.Failure<Option<SyncRoot>, ErrorResponse>(new ErrorResponse("locked"));

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.RootUnavailable).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_remote_index_is_not_complete_then_the_pass_fails_and_deletes_nothing()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a");
        fixture.Remote.Complete = false;

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.Unexpected).ShouldBeTrue();
        fixture.FileSystem.File.Exists("/sync/root/Docs/a.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_remote_index_cannot_be_read_then_the_pass_fails_and_deletes_nothing()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a");
        fixture.Remote.Fails = true;

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.Unexpected).ShouldBeTrue();
        fixture.FileSystem.File.Exists("/sync/root/Docs/a.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_synced_file_is_missing_locally_without_a_reported_deletion_then_it_is_repaired()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a", "c1");
        fixture.FileSystem.File.Delete("/sync/root/Docs/a.txt");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5);
        fixture.Downloader.Content["a"] = Hello;

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        summary.Downloaded.ShouldBe(1);
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/a.txt").ShouldBe("hello");
    }

    [Fact]
    public async Task when_both_sides_changed_a_file_then_it_is_left_alone_and_sent_to_conflicts()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a", "c1");
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);

        var summary = Summary(await fixture.RunAsync(Changes(LocalChangeFactory.CreateModified(Path("Docs/a.txt"), 7, DateTimeOffset.UtcNow, RemoteIds.Hash("h"))), CancellationToken));

        summary.SentToConflicts.ShouldBe(1);
        fixture.Downloader.Calls.ShouldBeEmpty();
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/a.txt").ShouldBe("hello");
    }

    [Fact]
    public async Task when_the_pass_runs_then_logs_carry_counts_and_no_paths_or_names()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/secret-name.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Hello;

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Logs.Logger.Messages.ShouldNotBeEmpty();
        fixture.Logs.Logger.Messages.ShouldNotContain(message => message.Contains("secret-name", StringComparison.Ordinal) || message.Contains("/sync/root", StringComparison.Ordinal));
    }
}
