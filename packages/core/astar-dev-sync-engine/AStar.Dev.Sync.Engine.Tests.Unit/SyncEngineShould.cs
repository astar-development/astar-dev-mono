using AStar.Dev.Functional.Extensions;
using AStar.Dev.Sync.Engine;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AStar.Dev.Sync.Engine.Tests.Unit;

public sealed class SyncEngineShould
{
    private readonly ISyncProvider _provider = Substitute.For<ISyncProvider>();
    private readonly SyncGate _lock = new();
    private readonly SyncOptions _options = new() { MaxConcurrency = 4 };
    private readonly ILogger<SyncEngine> _logger = Substitute.For<ILogger<SyncEngine>>();

    private readonly Dictionary<string, AccountSyncOptions> _accountOptions = new() { ["acct-1"] = new AccountSyncOptions { AccountId = "acct-1", SelectedFolders = ["Documents"] } };

    private SyncEngine CreateSut() => new(_provider, _lock, _accountOptions, _options, _logger);

    [Fact]
    public async Task ReturnErrorForUnknownAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        var result = await sut.SyncAsync("unknown", ct);

        result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task ReturnErrorWhenLockAlreadyHeld()
    {
        var ct = TestContext.Current.CancellationToken;
        _lock.TryAcquire("acct-1");
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task ReleaseLockAfterSync()
    {
        var ct = TestContext.Current.CancellationToken;
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([]));
        var sut = CreateSut();

        await sut.SyncAsync("acct-1", ct);

        _lock.IsRunning("acct-1").ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnEmptyReportWhenNoChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([]));
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        var ok = result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Ok>();
        ok.Value.ItemResults.ShouldBeEmpty();
        ok.Value.AccountId.ShouldBe("acct-1");
    }

    [Fact]
    public async Task UploadLocalToRemoteItems()
    {
        var ct = TestContext.Current.CancellationToken;
        var item = new SyncItem { RelativePath = "file.txt", Direction = SyncDirection.LocalToRemote };
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([item]));
        _provider.UploadAsync("acct-1", item, Arg.Any<CancellationToken>()).Returns(new SyncItemResult { Item = item, Succeeded = true });
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        var ok = result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Ok>();
        ok.Value.Uploaded.ShouldBe(1);
        ok.Value.Downloaded.ShouldBe(0);
    }

    [Fact]
    public async Task DownloadRemoteToLocalItems()
    {
        var ct = TestContext.Current.CancellationToken;
        var item = new SyncItem { RelativePath = "file.txt", Direction = SyncDirection.RemoteToLocal };
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([item]));
        _provider.DownloadAsync("acct-1", item, Arg.Any<CancellationToken>()).Returns(new SyncItemResult { Item = item, Succeeded = true });
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        var ok = result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Ok>();
        ok.Value.Downloaded.ShouldBe(1);
        ok.Value.Uploaded.ShouldBe(0);
    }

    [Fact]
    public async Task ReportFailedTransfers()
    {
        var ct = TestContext.Current.CancellationToken;
        var item = new SyncItem { RelativePath = "file.txt", Direction = SyncDirection.LocalToRemote };
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([item]));
        _provider.UploadAsync("acct-1", item, Arg.Any<CancellationToken>()).Returns(new SyncItemResult { Item = item, Succeeded = false, ErrorMessage = "disk full" });
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        var ok = result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Ok>();
        ok.Value.Failed.ShouldBe(1);
    }

    [Fact]
    public async Task CaptureExceptionFromProviderAsFailedItem()
    {
        var ct = TestContext.Current.CancellationToken;
        var item = new SyncItem { RelativePath = "file.txt", Direction = SyncDirection.LocalToRemote };
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([item]));
        _provider.UploadAsync("acct-1", item, Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("network error"));
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        var ok = result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Ok>();
        ok.Value.Failed.ShouldBe(1);
        ok.Value.ItemResults[0].ErrorMessage.ShouldBe("network error");
    }

    [Fact]
    public async Task ReturnErrorWhenGetChangesThrowsIOException()
    {
        var ct = TestContext.Current.CancellationToken;
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("disk error"));
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Error>();
        _lock.IsRunning("acct-1").ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnErrorOnCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", cts.Token);

        result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task UseAccountSpecificConcurrency()
    {
        var ct = TestContext.Current.CancellationToken;
        _accountOptions["acct-1"] = new AccountSyncOptions { AccountId = "acct-1", MaxConcurrency = 2, SelectedFolders = ["Docs"] };
        var items = Enumerable.Range(0, 5).Select(i => new SyncItem { RelativePath = $"f{i}.txt", Direction = SyncDirection.RemoteToLocal }).ToList();
        var maxConcurrent = 0;
        var currentConcurrent = 0;

        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>(items));
        _provider.DownloadAsync("acct-1", Arg.Any<SyncItem>(), Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            var current = Interlocked.Increment(ref currentConcurrent);
            InterlockedMax(ref maxConcurrent, current);
            await Task.Delay(50, ct);
            Interlocked.Decrement(ref currentConcurrent);

            return new SyncItemResult { Item = callInfo.Arg<SyncItem>(), Succeeded = true };
        });

        var sut = CreateSut();
        await sut.SyncAsync("acct-1", ct);

        maxConcurrent.ShouldBeInRange(1, 2);
    }

    [Fact]
    public async Task SyncBidirectionalItems()
    {
        var ct = TestContext.Current.CancellationToken;
        var upload = new SyncItem { RelativePath = "local.txt", Direction = SyncDirection.LocalToRemote };
        var download = new SyncItem { RelativePath = "remote.txt", Direction = SyncDirection.RemoteToLocal };
        _provider.GetChangesAsync("acct-1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncItem>>([upload, download]));
        _provider.UploadAsync("acct-1", upload, Arg.Any<CancellationToken>()).Returns(new SyncItemResult { Item = upload, Succeeded = true });
        _provider.DownloadAsync("acct-1", download, Arg.Any<CancellationToken>()).Returns(new SyncItemResult { Item = download, Succeeded = true });
        var sut = CreateSut();

        var result = await sut.SyncAsync("acct-1", ct);

        var ok = result.ShouldBeOfType<Result<SyncReport, ErrorResponse>.Ok>();
        ok.Value.Uploaded.ShouldBe(1);
        ok.Value.Downloaded.ShouldBe(1);
        ok.Value.Failed.ShouldBe(0);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;

        do
        {
            current = location;

            if (value <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
