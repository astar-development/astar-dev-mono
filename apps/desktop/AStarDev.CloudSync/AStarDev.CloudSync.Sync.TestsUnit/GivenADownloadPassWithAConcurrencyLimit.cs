using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPassWithAConcurrencyLimit
{
    private static readonly byte[] Hello = "hello"u8.ToArray();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<DownloadPassFixture> FixtureWithFilesAsync(int files)
    {
        var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Downloader.Delay = TimeSpan.FromMilliseconds(60);
        foreach (var index in Enumerable.Range(0, files))
        {
            fixture.Remote.File($"Docs/f{index}.txt", $"id{index}", "c2", 5);
            fixture.Downloader.Content[$"id{index}"] = Hello;
        }

        return fixture;
    }

    [Fact]
    public async Task when_the_account_allows_two_transfers_then_no_more_than_two_downloads_run_at_once()
    {
        using var fixture = await FixtureWithFilesAsync(10);
        fixture.Concurrency.Value = 2;

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Downloader.MostRunning.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_concurrency_cannot_be_read_then_the_default_of_four_is_used_and_the_failure_is_logged()
    {
        using var fixture = await FixtureWithFilesAsync(12);
        fixture.Concurrency.Fails = true;

        _ = await fixture.RunAsync(CancellationToken);

        (fixture.Downloader.MostRunning, fixture.Logs.Logger.Messages.Any(message => message.Contains("could not be read", StringComparison.Ordinal))).ShouldBe((4, true));
    }

    [Fact]
    public async Task when_a_pass_runs_then_the_accounts_concurrency_is_read_once_at_its_start()
    {
        using var fixture = await FixtureWithFilesAsync(6);

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Concurrency.Reads.ShouldBe(1);
    }
}
