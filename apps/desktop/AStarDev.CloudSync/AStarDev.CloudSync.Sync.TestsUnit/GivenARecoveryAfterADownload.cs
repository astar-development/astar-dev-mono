using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenARecoveryAfterADownload
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_app_died_after_a_file_downloaded_then_recovery_does_not_download_it_again_and_fetches_only_what_is_left()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c-downloaded", 10);
        fixture.Downloader.Content["a"] = new byte[10];
        var reached = 0;
        fixture.Steps.OnStep = () =>
        {
            if (++reached == 3)
                fixture.Journal.Kill();
        };
        _ = await fixture.RunSyncAsync(CancellationToken);
        fixture.Journal.Restart();
        fixture.Remote.File("Docs/b.txt", "b", "c-downloaded", 10);
        fixture.Downloader.Content["b"] = new byte[10];

        var outcomes = await fixture.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldHaveSingleItem().ShouldBeOfType<RecoveryOutcome.Resumed>();
        fixture.Downloader.Calls.ShouldBe(["a", "b"]);
    }
}
