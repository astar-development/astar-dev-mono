using AStar.Dev.Conflict.Resolution.Features.Detection;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.DeltaQueries;
using AStar.Dev.Sync.Engine.Features.Concurrency;
using AStar.Dev.Sync.Engine.Features.DiskSpace;
using AStar.Dev.Sync.Engine.Features.FileTransfer;
using AStar.Dev.Sync.Engine.Features.LocalScanning;
using AStar.Dev.Sync.Engine.Features.ProgressTracking;
using AStar.Dev.Sync.Engine.Features.Resilience;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.DiskSpace;

public sealed class GivenADiskSpaceCheck : IDisposable
{
    private readonly SyncGate _gate = new();
    private readonly ISyncStateStore _stateStore = Substitute.For<ISyncStateStore>();
    private readonly ISyncProgressReporter _progressReporter = Substitute.For<ISyncProgressReporter>();
    private readonly IDeltaQueryService _deltaQueryService = Substitute.For<IDeltaQueryService>();
    private readonly IFileTransferService _fileTransferService = Substitute.For<IFileTransferService>();
    private readonly ILocalFileScanner _localFileScanner = Substitute.For<ILocalFileScanner>();
    private readonly IDiskSpaceChecker _diskSpaceChecker = Substitute.For<IDiskSpaceChecker>();
    private readonly IDbBackupService _dbBackupService = Substitute.For<IDbBackupService>();

    private SyncEngine BuildEngine() => new(_gate, _stateStore, _progressReporter, _deltaQueryService, _fileTransferService, _localFileScanner, _diskSpaceChecker, _dbBackupService, new ExponentialBackoffPolicy(NullLogger<ExponentialBackoffPolicy>.Instance), new System.IO.Abstractions.TestingHelpers.MockFileSystem(), Substitute.For<IConflictDetector>(), Substitute.For<IConflictStore>(), NullLogger<SyncEngine>.Instance);

    [Fact]
    public async Task when_available_space_is_less_than_required_then_insufficient_disk_space_error_is_returned()
    {
        const string accountId = "account-disk-test";
        const long availableBytes = 100L;
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);

        var deltaItems = new List<DeltaItem> { DeltaItemFactory.Create("f1", "file.txt", null, DeltaItemType.File) };
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "delta-token", false);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(availableBytes);

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Error>();
        var error = ((Result<SyncReport, SyncEngineError>.Error)result).Reason;
        error.ShouldBeOfType<InsufficientDiskSpaceError>();

        var diskError = (InsufficientDiskSpaceError)error;
        diskError.AvailableBytes.ShouldBe(availableBytes);
    }

    [Fact]
    public async Task when_available_space_exceeds_required_bytes_then_sync_proceeds()
    {
        const string accountId = "account-has-space";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);

        var deltaItems = new List<DeltaItem>();
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "delta-token", false);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Ok>();
    }

    public void Dispose() => _gate.Dispose();
}
