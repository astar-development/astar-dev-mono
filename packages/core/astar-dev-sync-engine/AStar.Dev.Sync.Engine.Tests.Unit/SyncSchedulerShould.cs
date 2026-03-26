using AStar.Dev.Functional.Extensions;
using AStar.Dev.Sync.Engine;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine.Tests.Unit;

public sealed class SyncSchedulerShould
{
    private readonly ISyncEngine _engine = Substitute.For<ISyncEngine>();
    private readonly SyncGate _lock = new();
    private readonly ILogger<SyncScheduler> _logger = Substitute.For<ILogger<SyncScheduler>>();

    private SyncScheduler CreateSut(TimeSpan? interval = null)
    {
        var options = new SyncOptions { SyncInterval = interval ?? TimeSpan.FromMilliseconds(100) };

        return new SyncScheduler(_engine, _lock, options, _logger);
    }

    [Fact]
    public async Task ReportIsRunningAfterStart()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();
        await sut.StartAsync(["acct-1"], ct);

        sut.IsRunning.ShouldBeTrue();

        await sut.StopAsync();
    }

    [Fact]
    public async Task ReportNotRunningAfterStop()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();
        await sut.StartAsync(["acct-1"], ct);
        await sut.StopAsync();

        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task NotStartWithEmptyAccounts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();
        await sut.StartAsync([], ct);

        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeSyncForScheduledAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        var report = new SyncReport { AccountId = "acct-1", StartedAtUtc = DateTimeOffset.UtcNow, CompletedAtUtc = DateTimeOffset.UtcNow, ItemResults = [] };
        _engine.SyncAsync("acct-1", Arg.Any<CancellationToken>()).Returns(new Result<SyncReport, ErrorResponse>.Ok(report));

        await using var sut = CreateSut(TimeSpan.FromMilliseconds(50));
        await sut.StartAsync(["acct-1"], ct);

        await Task.Delay(200, ct);
        await sut.StopAsync();

        await _engine.Received().SyncAsync("acct-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipAccountWhenLockIsHeld()
    {
        var ct = TestContext.Current.CancellationToken;
        _lock.TryAcquire("acct-1");

        await using var sut = CreateSut(TimeSpan.FromMilliseconds(50));
        await sut.StartAsync(["acct-1"], ct);

        await Task.Delay(200, ct);
        await sut.StopAsync();
        _lock.Release("acct-1");

        await _engine.DidNotReceive().SyncAsync("acct-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopGracefullyViaDispose()
    {
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        await sut.StartAsync(["acct-1"], ct);

        await sut.DisposeAsync();

        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task ThrowWhenAccountIdsIsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();

        await Should.ThrowAsync<ArgumentNullException>(async () => await sut.StartAsync(null!, ct));
    }

    [Fact]
    public async Task NotThrowWhenStoppingAlreadyStoppedScheduler()
    {
        await using var sut = CreateSut();

        await Should.NotThrowAsync(async () => await sut.StopAsync());
    }

    [Fact]
    public async Task NotStartTwice()
    {
        var ct = TestContext.Current.CancellationToken;
        var report = new SyncReport { AccountId = "acct-1", StartedAtUtc = DateTimeOffset.UtcNow, CompletedAtUtc = DateTimeOffset.UtcNow, ItemResults = [] };
        _engine.SyncAsync("acct-1", Arg.Any<CancellationToken>()).Returns(new Result<SyncReport, ErrorResponse>.Ok(report));

        await using var sut = CreateSut(TimeSpan.FromMilliseconds(50));
        await sut.StartAsync(["acct-1"], ct);
        await sut.StartAsync(["acct-1"], ct);

        sut.IsRunning.ShouldBeTrue();
        await sut.StopAsync();
    }
}
