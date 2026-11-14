using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALocalTrash : IDisposable
{
    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly MockFileSystem windows = new(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider provider;

    public GivenALocalTrash() => provider = new ServiceCollection().AddSingleton<System.TimeProvider>(time).AddCloudSync().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ILocalFileOperations Files => provider.GetRequiredService<ILocalFileOperations>();

    private static string Outcome(Result<Unit, LocalOperationFailure> result) => result.Match(_ => "ok", failure => failure.GetType().Name);

    private static string[] TrashedFiles(IFileSystem system, string root) =>
        system.Directory.Exists($"{root}/.sync-trash") ? system.Directory.GetFiles($"{root}/.sync-trash", "*", SearchOption.AllDirectories) : [];

    private async Task<Result<Unit, LocalOperationFailure>> SettledAsync(Task<Result<Unit, LocalOperationFailure>> running)
    {
        while (!running.IsCompleted)
        {
            time.Advance(TimeSpan.FromMilliseconds(200));
            await Task.Delay(1, CancellationToken);
        }

        return await running;
    }

    public void Dispose() => provider.Dispose();

    [Fact]
    public async Task when_a_file_is_trashed_then_it_moves_into_the_sync_trash_keeping_its_relative_path_and_content()
    {
        fileSystem.Directory.CreateDirectory("/root/Docs");
        fileSystem.File.WriteAllText("/root/Docs/a.txt", "hello");

        var result = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/root/Docs/a.txt"), CancellationToken);

        Outcome(result).ShouldBe("ok");
        fileSystem.File.Exists("/root/Docs/a.txt").ShouldBeFalse();
        var trashed = TrashedFiles(fileSystem, "/root").ShouldHaveSingleItem();
        trashed.ShouldEndWith("/Docs/a.txt");
        fileSystem.File.ReadAllText(trashed).ShouldBe("hello");
    }

    [Fact]
    public async Task when_a_folder_is_trashed_then_the_whole_folder_with_its_content_moves()
    {
        fileSystem.Directory.CreateDirectory("/root/Docs/Sub");
        fileSystem.File.WriteAllText("/root/Docs/Sub/a.txt", "a");
        fileSystem.File.WriteAllText("/root/Docs/b.txt", "b");

        var result = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.DirectoryInfo.New("/root/Docs"), CancellationToken);

        Outcome(result).ShouldBe("ok");
        fileSystem.Directory.Exists("/root/Docs").ShouldBeFalse();
        TrashedFiles(fileSystem, "/root").Select(path => path[path.IndexOf("/Docs/", StringComparison.Ordinal)..]).Order().ShouldBe(["/Docs/Sub/a.txt", "/Docs/b.txt"]);
    }

    [Fact]
    public async Task when_the_item_is_already_gone_then_trashing_succeeds_and_creates_nothing()
    {
        fileSystem.Directory.CreateDirectory("/root");

        var result = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/root/none.txt"), CancellationToken);

        Outcome(result).ShouldBe("ok");
        TrashedFiles(fileSystem, "/root").ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_item_is_already_inside_the_trash_then_it_is_not_trashed_again()
    {
        fileSystem.Directory.CreateDirectory("/root/.sync-trash/old");
        fileSystem.File.WriteAllText("/root/.sync-trash/old/a.txt", "kept");

        var result = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/root/.sync-trash/old/a.txt"), CancellationToken);

        Outcome(result).ShouldBe("ok");
        TrashedFiles(fileSystem, "/root").ShouldBe(["/root/.sync-trash/old/a.txt"]);
        fileSystem.File.ReadAllText("/root/.sync-trash/old/a.txt").ShouldBe("kept");
    }

    [Fact]
    public async Task when_the_item_is_in_a_differently_cased_trash_folder_then_it_is_not_trashed_again()
    {
        fileSystem.Directory.CreateDirectory("/root/.SYNC-TRASH/old");
        fileSystem.File.WriteAllText("/root/.SYNC-TRASH/old/a.txt", "kept");

        var result = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/root/.SYNC-TRASH/old/a.txt"), CancellationToken);

        Outcome(result).ShouldBe("ok");
        fileSystem.File.Exists("/root/.SYNC-TRASH/old/a.txt").ShouldBeTrue();
        fileSystem.Directory.Exists("/root/.sync-trash").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_item_is_outside_the_root_then_it_is_left_alone_and_the_move_fails()
    {
        fileSystem.Directory.CreateDirectory("/root");
        fileSystem.Directory.CreateDirectory("/elsewhere");
        fileSystem.File.WriteAllText("/elsewhere/a.txt", "a");

        var result = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/elsewhere/a.txt"), CancellationToken);

        Outcome(result).ShouldBe("Unexpected");
        fileSystem.File.Exists("/elsewhere/a.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_same_name_is_trashed_twice_then_both_copies_are_kept()
    {
        fileSystem.Directory.CreateDirectory("/root");
        fileSystem.File.WriteAllText("/root/a.txt", "first");
        _ = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/root/a.txt"), CancellationToken);
        fileSystem.File.WriteAllText("/root/a.txt", "second");

        _ = await Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New("/root/a.txt"), CancellationToken);

        TrashedFiles(fileSystem, "/root").Select(fileSystem.File.ReadAllText).Order().ShouldBe(["first", "second"]);
    }

    [Fact]
    public async Task when_a_case_variant_is_trashed_on_a_case_insensitive_volume_then_the_earlier_copy_is_kept()
    {
        windows.Directory.CreateDirectory(@"C:\root");
        windows.File.WriteAllText(@"C:\root\A.txt", "first");
        _ = await Files.TrashAsync(windows.DirectoryInfo.New(@"C:\root"), windows.FileInfo.New(@"C:\root\A.txt"), CancellationToken);
        windows.File.WriteAllText(@"C:\root\a.txt", "second");

        var result = await Files.TrashAsync(windows.DirectoryInfo.New(@"C:\root"), windows.FileInfo.New(@"C:\root\a.txt"), CancellationToken);

        Outcome(result).ShouldBe("ok");
        windows.Directory.GetFiles(@"C:\root\.sync-trash", "*", SearchOption.AllDirectories).Select(windows.File.ReadAllText).Order().ShouldBe(["first", "second"]);
    }

    [Fact]
    public async Task when_twenty_case_variants_are_trashed_in_parallel_then_every_copy_is_kept()
    {
        fileSystem.Directory.CreateDirectory("/root");
        var names = Enumerable.Range(0, 20).Select(index => string.Concat("abcde".Select((letter, position) => ((index >> position) & 1) == 1 ? char.ToUpperInvariant(letter) : letter)) + ".txt").ToList();
        names.ForEach(name => fileSystem.File.WriteAllText($"/root/{name}", name));

        var results = await Task.WhenAll(names.Select(name => Task.Run(() => Files.TrashAsync(fileSystem.DirectoryInfo.New("/root"), fileSystem.FileInfo.New($"/root/{name}"), CancellationToken), CancellationToken)));

        results.Select(Outcome).Distinct().ShouldBe(["ok"]);
        TrashedFiles(fileSystem, "/root").Select(fileSystem.File.ReadAllText).Order(StringComparer.Ordinal).ShouldBe(names.Order(StringComparer.Ordinal));
        fileSystem.Directory.GetFiles("/root").ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_file_is_locked_then_the_move_retries_reports_locked_and_never_deletes()
    {
        windows.Directory.CreateDirectory(@"C:\root");
        windows.File.WriteAllText(@"C:\root\a.txt", "old");
        using var holder = windows.FileInfo.New(@"C:\root\a.txt").Open(FileMode.Open, FileAccess.Read, FileShare.None);

        var result = await SettledAsync(Files.TrashAsync(windows.DirectoryInfo.New(@"C:\root"), windows.FileInfo.New(@"C:\root\a.txt"), CancellationToken));

        Outcome(result).ShouldBe("Locked");
        windows.File.Exists(@"C:\root\a.txt").ShouldBeTrue();
        TrashedFiles(windows, @"C:\root").ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_retry_wait_is_cancelled_then_the_cancellation_propagates()
    {
        windows.Directory.CreateDirectory(@"C:\root");
        windows.File.WriteAllText(@"C:\root\a.txt", "old");
        using var holder = windows.FileInfo.New(@"C:\root\a.txt").Open(FileMode.Open, FileAccess.Read, FileShare.None);
        using var cancellation = new CancellationTokenSource();

        var running = Files.TrashAsync(windows.DirectoryInfo.New(@"C:\root"), windows.FileInfo.New(@"C:\root\a.txt"), cancellation.Token);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => running);
    }

    [Fact]
    public void when_the_retention_is_created_then_the_default_is_thirty_days_and_a_negative_value_becomes_zero()
    {
        (LocalTrashOptionsFactory.Create().Retention, LocalTrashOptionsFactory.Create(TimeSpan.FromDays(-1)).Retention, LocalTrashOptionsFactory.Create(TimeSpan.FromDays(7)).Retention)
            .ShouldBe((TimeSpan.FromDays(30), TimeSpan.Zero, TimeSpan.FromDays(7)));
    }

    [Fact]
    public void when_the_services_are_registered_then_the_trash_options_default_to_thirty_days() =>
        provider.GetRequiredService<LocalTrashOptions>().Retention.ShouldBe(TimeSpan.FromDays(30));
}
