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

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.StateTracking;

public sealed class GivenAnInterruptedSync : IDisposable
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
    public async Task when_previous_state_is_interrupted_and_checkpoint_exists_then_sync_resumes()
    {
        const string accountId = "account-interrupted";
        var ct = TestContext.Current.CancellationToken;
        var checkpoint = SyncCheckpointFactory.Create(accountId, "last-file-id");

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)SyncAccountState.Interrupted);
        _stateStore.GetCheckpointAsync(accountId, Arg.Any<CancellationToken>()).Returns(checkpoint);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var deltaItems = new List<DeltaItem>();
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "delta-token", false);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Ok>();
    }

    [Fact]
    public async Task when_previous_state_is_interrupted_and_checkpoint_is_null_then_resume_failed_error_is_returned()
    {
        const string accountId = "account-corrupt-checkpoint";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)SyncAccountState.Interrupted);
        _stateStore.GetCheckpointAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncCheckpoint?)null);

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Error>();
        var error = ((Result<SyncReport, SyncEngineError>.Error)result).Reason;
        error.ShouldBeOfType<ResumeFailedError>();
    }

    [Fact]
    public async Task when_sync_completes_successfully_then_state_is_set_to_completed()
    {
        const string accountId = "account-completes";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var deltaItems = new List<DeltaItem>();
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "delta-token", false);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        await BuildEngine().StartSyncAsync(accountId, ct: ct);

        await _stateStore.Received(1).SetStateAsync(accountId, SyncAccountState.Completed, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).ClearCheckpointAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_delta_token_is_expired_then_full_resync_required_error_is_returned()
    {
        const string accountId = "account-expired-token";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);

        var expiredError = DeltaQueryErrorFactory.TokenExpired();
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Error(expiredError));

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Error>();
        ((Result<SyncReport, SyncEngineError>.Error)result).Reason.ShouldBeOfType<FullResyncRequiredError>();
    }

    [Fact]
    public async Task when_same_account_is_already_syncing_then_already_running_error_is_returned()
    {
        const string accountId = "account-already-running";
        var ct = TestContext.Current.CancellationToken;

        _gate.TryBeginSync(accountId);

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Error>();
        ((Result<SyncReport, SyncEngineError>.Error)result).Reason.ShouldBeOfType<SyncAlreadyRunningError>();
    }

    public void Dispose() => _gate.Dispose();
}
