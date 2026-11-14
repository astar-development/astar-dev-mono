using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPassThatTrashes
{
    private const string Trash = "/sync/root/.sync-trash";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string Stamp(DateTimeOffset moment) => moment.UtcDateTime.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public async Task when_the_remote_file_was_deleted_then_the_local_copy_is_in_the_trash_and_not_gone_for_good()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Synced("Docs/a.txt", "hello", "a");
        fixture.Remote.Folder("Docs");

        var summary = DownloadPassFixture.Summary(await fixture.RunAsync(CancellationToken));

        summary.DeletedLocally.ShouldBe(1);
        fixture.FileSystem.File.Exists("/sync/root/Docs/a.txt").ShouldBeFalse();
        fixture.FileSystem.Directory.GetFiles(Trash, "a.txt", SearchOption.AllDirectories).Select(fixture.FileSystem.File.ReadAllText).ShouldBe(["hello"]);
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_remote_folder_was_deleted_then_the_folder_is_trashed_and_its_baseline_row_removed()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.SyncedFolder("Docs/Old", "old");
        fixture.Remote.Folder("Docs");

        var summary = DownloadPassFixture.Summary(await fixture.RunAsync(CancellationToken));

        summary.DeletedLocally.ShouldBe(1);
        fixture.FileSystem.Directory.Exists("/sync/root/Docs/Old").ShouldBeFalse();
        fixture.FileSystem.Directory.GetDirectories(Trash, "Old", SearchOption.AllDirectories).ShouldHaveSingleItem();
        fixture.Baseline.Has("Docs/Old").ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_pass_starts_then_expired_trash_entries_are_removed_and_recent_and_unrelated_ones_are_kept()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs");
        var now = fixture.Time.GetUtcNow();
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/{Stamp(now.AddDays(-31))}-aaaa");
        fixture.FileSystem.File.WriteAllText($"{Trash}/{Stamp(now.AddDays(-31))}-aaaa/old.txt", "old");
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/{Stamp(now.AddDays(-29))}-bbbb");
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/not-a-stamp");

        _ = await fixture.RunAsync(CancellationToken);

        fixture.FileSystem.Directory.GetDirectories(Trash).Select(path => path[(Trash.Length + 1)..]).Order().ShouldBe([$"{Stamp(now.AddDays(-29))}-bbbb", "not-a-stamp"]);
    }

    [Fact]
    public async Task when_the_retention_is_shorter_then_entries_older_than_it_are_removed()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken, trashOptions: LocalTrashOptionsFactory.Create(TimeSpan.FromDays(2)));
        fixture.Docs();
        fixture.Remote.Folder("Docs");
        var now = fixture.Time.GetUtcNow();
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/{Stamp(now.AddDays(-3))}-aaaa");

        _ = await fixture.RunAsync(CancellationToken);

        fixture.FileSystem.Directory.GetDirectories(Trash).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_setting_is_changed_between_passes_then_the_next_pass_uses_the_new_retention()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs");
        var now = fixture.Time.GetUtcNow();
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/{Stamp(now.AddDays(-10))}-aaaa");
        _ = await fixture.RunAsync(CancellationToken);
        var keptAtThirtyDays = fixture.FileSystem.Directory.GetDirectories(Trash).Length;

        _ = await fixture.TrashRetention.SetAsync(LocalTrashOptionsFactory.CreateFromDays(7), CancellationToken);
        _ = await fixture.RunAsync(CancellationToken);

        keptAtThirtyDays.ShouldBe(1);
        fixture.FileSystem.Directory.GetDirectories(Trash).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_setting_cannot_be_read_then_the_default_retention_applies()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs");
        fixture.TrashRetention.Current = Result.Failure<LocalTrashOptions, ErrorResponse>(new ErrorResponse("unreadable"));
        var now = fixture.Time.GetUtcNow();
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/{Stamp(now.AddDays(-31))}-aaaa");
        fixture.FileSystem.Directory.CreateDirectory($"{Trash}/{Stamp(now.AddDays(-29))}-bbbb");

        _ = await fixture.RunAsync(CancellationToken);

        fixture.FileSystem.Directory.GetDirectories(Trash).Select(path => path[(Trash.Length + 1)..]).ShouldBe([$"{Stamp(now.AddDays(-29))}-bbbb"]);
    }

    [Fact]
    public async Task when_there_is_no_trash_then_a_pass_does_not_create_one()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs");

        _ = await fixture.RunAsync(CancellationToken);

        fixture.FileSystem.Directory.Exists(Trash).ShouldBeFalse();
    }
}
