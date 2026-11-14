using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPassThatSkipsTheSyncTrash
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<DownloadPassFixture> FixtureWithTrashOnTheRemoteAsync()
    {
        var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([SelectionTestRules.Include("Docs"), SelectionTestRules.Include(".sync-trash")]);
        fixture.Docs();
        fixture.Remote.Folder("Docs");
        fixture.Downloader.Content["keep"] = System.Text.Encoding.UTF8.GetBytes("data");
        fixture.Remote.File("Docs/keep.txt", "keep", "c1", 4);
        fixture.Remote.File(".sync-trash/20261113120000000-abc/secret.txt", "secret", "c1", 6);

        return fixture;
    }

    [Fact]
    public async Task when_the_remote_holds_sync_trash_items_then_nothing_of_them_is_downloaded_or_created()
    {
        using var fixture = await FixtureWithTrashOnTheRemoteAsync();

        var summary = DownloadPassFixture.Summary(await fixture.RunAsync(CancellationToken));

        summary.Downloaded.ShouldBe(1);
        fixture.FileSystem.Directory.Exists("/sync/root/.sync-trash").ShouldBeFalse();
        fixture.FileSystem.Directory.GetFiles("/sync/root", "secret.txt", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_remote_holds_sync_trash_items_then_no_baseline_row_is_written_for_them()
    {
        using var fixture = await FixtureWithTrashOnTheRemoteAsync();

        _ = await fixture.RunAsync(CancellationToken);

        (fixture.Baseline.Has("Docs/keep.txt"), fixture.Baseline.Has(".sync-trash"), fixture.Baseline.Has(".sync-trash/20261113120000000-abc/secret.txt")).ShouldBe((true, false, false));
    }

    [Fact]
    public async Task when_the_remote_holds_sync_trash_items_then_only_their_count_is_logged()
    {
        using var fixture = await FixtureWithTrashOnTheRemoteAsync();

        _ = await fixture.RunAsync(CancellationToken);

        var messages = fixture.Logs.Logger.Messages;
        (messages.Any(message => message.Contains("skipped 3 remote items under the sync trash", StringComparison.Ordinal)), messages.Any(message => message.Contains("secret", StringComparison.Ordinal) || message.Contains("20261113", StringComparison.Ordinal))).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_remote_holds_no_sync_trash_items_then_no_skip_is_logged()
    {
        using var fixture = await DownloadPassFixture.CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.Folder("Docs");

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Logs.Logger.Messages.ShouldAllBe(message => !message.Contains("under the sync trash", StringComparison.Ordinal));
    }
}
