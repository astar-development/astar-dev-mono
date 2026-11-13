using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALocalFileOperations : IDisposable
{
    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly MockFileSystem windows = new(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider provider;

    public GivenALocalFileOperations() => provider = new ServiceCollection().AddSingleton<System.TimeProvider>(time).AddCloudSync().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ILocalFileOperations Files => provider.GetRequiredService<ILocalFileOperations>();

    private async Task<Result<Unit, LocalOperationFailure>> SettledAsync(Task<Result<Unit, LocalOperationFailure>> running)
    {
        while (!running.IsCompleted)
        {
            time.Advance(TimeSpan.FromMilliseconds(200));
            await Task.Delay(1, CancellationToken);
        }

        return await running;
    }

    private static string Outcome(Result<Unit, LocalOperationFailure> result) => result.Match(_ => "ok", failure => failure.GetType().Name);

    [Fact]
    public async Task when_a_folder_is_created_then_missing_parents_are_created_and_an_existing_folder_is_not_an_error()
    {
        var folder = fileSystem.DirectoryInfo.New("/root/a/b");

        var first = await Files.CreateFolderAsync(folder, CancellationToken);
        var second = await Files.CreateFolderAsync(folder, CancellationToken);

        (Outcome(first), Outcome(second)).ShouldBe(("ok", "ok"));
        fileSystem.Directory.Exists("/root/a/b").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_file_is_moved_then_the_target_parent_is_created_and_nothing_is_overwritten()
    {
        fileSystem.Directory.CreateDirectory("/root");
        fileSystem.File.WriteAllText("/root/a.txt", "a");
        fileSystem.File.WriteAllText("/root/b.txt", "b");

        var moved = await Files.MoveAsync(fileSystem.FileInfo.New("/root/a.txt"), "/root/sub/c.txt", CancellationToken);
        var clash = await SettledAsync(Files.MoveAsync(fileSystem.FileInfo.New("/root/sub/c.txt"), "/root/b.txt", CancellationToken));

        Outcome(moved).ShouldBe("ok");
        Outcome(clash).ShouldBe("Locked");
        fileSystem.File.ReadAllText("/root/b.txt").ShouldBe("b");
        fileSystem.File.Exists("/root/sub/c.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_folder_is_moved_then_its_content_comes_with_it()
    {
        fileSystem.Directory.CreateDirectory("/root/old");
        fileSystem.File.WriteAllText("/root/old/a.txt", "a");

        var moved = await Files.MoveAsync(fileSystem.DirectoryInfo.New("/root/old"), "/root/new", CancellationToken);

        Outcome(moved).ShouldBe("ok");
        fileSystem.File.ReadAllText("/root/new/a.txt").ShouldBe("a");
    }

    [Fact]
    public async Task when_an_item_is_deleted_then_a_file_an_empty_folder_and_a_missing_item_all_succeed()
    {
        fileSystem.Directory.CreateDirectory("/root/empty");
        fileSystem.File.WriteAllText("/root/a.txt", "a");

        var file = await Files.DeleteAsync(fileSystem.FileInfo.New("/root/a.txt"), CancellationToken);
        var folder = await Files.DeleteAsync(fileSystem.DirectoryInfo.New("/root/empty"), CancellationToken);
        var missing = await Files.DeleteAsync(fileSystem.FileInfo.New("/root/none.txt"), CancellationToken);

        (Outcome(file), Outcome(folder), Outcome(missing)).ShouldBe(("ok", "ok", "ok"));
        fileSystem.File.Exists("/root/a.txt").ShouldBeFalse();
        fileSystem.Directory.Exists("/root/empty").ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_folder_that_is_not_empty_is_deleted_then_it_fails_and_keeps_its_content()
    {
        fileSystem.Directory.CreateDirectory("/root/full");
        fileSystem.File.WriteAllText("/root/full/a.txt", "a");
        using var deleting = new CancellationTokenSource();
        var running = Files.DeleteAsync(fileSystem.DirectoryInfo.New("/root/full"), CancellationToken);
        while (!running.IsCompleted)
        {
            time.Advance(TimeSpan.FromMilliseconds(200));
            await Task.Delay(1, CancellationToken);
        }

        Outcome(await running).ShouldBe("Locked");
        fileSystem.File.Exists("/root/full/a.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_download_replaces_a_file_then_the_content_and_the_last_write_time_are_replaced()
    {
        fileSystem.Directory.CreateDirectory("/root");
        fileSystem.File.WriteAllText("/root/a.txt", "old");
        fileSystem.File.WriteAllText("/root/a.txt.x.download", "new");
        var stamp = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var replaced = await Files.ReplaceAsync(fileSystem.FileInfo.New("/root/a.txt.x.download"), fileSystem.FileInfo.New("/root/a.txt"), stamp, CancellationToken);

        Outcome(replaced).ShouldBe("ok");
        fileSystem.File.ReadAllText("/root/a.txt").ShouldBe("new");
        fileSystem.File.GetLastWriteTimeUtc("/root/a.txt").ShouldBe(stamp.UtcDateTime);
        fileSystem.File.Exists("/root/a.txt.x.download").ShouldBeFalse();
    }

    private IFileInfo LockedTarget(out Stream holder)
    {
        windows.Directory.CreateDirectory(@"C:\root");
        windows.File.WriteAllText(@"C:\root\a.txt", "old");
        windows.File.WriteAllText(@"C:\root\a.txt.x.download", "new");
        holder = windows.FileInfo.New(@"C:\root\a.txt").Open(FileMode.Open, FileAccess.Read, FileShare.None);

        return windows.FileInfo.New(@"C:\root\a.txt");
    }

    [Fact]
    public async Task when_the_file_is_locked_then_the_delete_retries_and_reports_locked_without_throwing()
    {
        var target = LockedTarget(out var holder);
        using var _ = holder;

        var result = await SettledAsync(Files.DeleteAsync(target, CancellationToken));

        Outcome(result).ShouldBe("Locked");
    }

    [Fact]
    public async Task when_the_file_is_released_during_the_retry_wait_then_the_delete_succeeds()
    {
        var target = LockedTarget(out var holder);
        var running = Files.DeleteAsync(target, CancellationToken);
        holder.Dispose();

        var result = await SettledAsync(running);

        Outcome(result).ShouldBe("ok");
        windows.File.Exists(@"C:\root\a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_retry_wait_is_cancelled_then_the_cancellation_propagates()
    {
        var target = LockedTarget(out var holder);
        using var _ = holder;
        using var cancellation = new CancellationTokenSource();

        var running = Files.DeleteAsync(target, cancellation.Token);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => running);
    }

    public void Dispose() => provider.Dispose();
}
