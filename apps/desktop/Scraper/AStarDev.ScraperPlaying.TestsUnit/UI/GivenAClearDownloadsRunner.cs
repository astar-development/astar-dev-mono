using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Downloads;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAClearDownloadsRunner : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly FakeDownloadsClearer downloadsClearer = new();
    private readonly ClearDownloadsRunner runner;

    public GivenAClearDownloadsRunner() => runner = new(downloadsClearer, new UserOperationRunner(coordinator, status), status);

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_the_downloads_are_cleared_then_the_user_is_told_how_many_file_records_were_removed()
    {
        downloadsClearer.Result = Exceptional.Success(new ClearedDownloads(7));

        await runner.RunAsync();

        (downloadsClearer.ClearCount, status.Text).ShouldBe((1, $"{TestStatusReporter.Timestamp} Downloads cleared: 7 file records removed and the save directories emptied."));
    }

    [Fact]
    public async Task when_clearing_fails_then_the_failure_is_reported()
    {
        downloadsClearer.Result = new InvalidOperationException("clear failed");

        await runner.RunAsync();

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to clear downloads. clear failed");
    }

    [Fact]
    public async Task when_another_operation_is_already_running_then_nothing_is_cleared()
    {
        _ = coordinator.TryStart(out _);

        await runner.RunAsync();

        (downloadsClearer.ClearCount, status.Text).ShouldBe((0, string.Empty));
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_then_the_user_is_told()
    {
        downloadsClearer.OnClear = () => coordinator.Cancel();

        await runner.RunAsync();

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Clear Downloads cancelled.");
    }

    private sealed class FakeDownloadsClearer : IDownloadsClearer
    {
        public Exceptional<ClearedDownloads> Result { get; set; } = Exceptional.Success(new ClearedDownloads(0));

        public Action OnClear { get; set; } = () => { };

        public int ClearCount { get; private set; }

        public Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;
            OnClear();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Result);
        }
    }
}
