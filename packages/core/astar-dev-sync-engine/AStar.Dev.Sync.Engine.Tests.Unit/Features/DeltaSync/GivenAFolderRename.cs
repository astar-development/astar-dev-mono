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

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.DeltaSync;

public sealed class GivenAFolderRename : IDisposable
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
    public async Task when_delta_contains_folder_renamed_item_then_sync_completes_without_downloading()
    {
        const string accountId = "account-folder-rename";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var renamedFolder = DeltaItemFactory.Create("folder-id", "NewFolderName", null, DeltaItemType.FolderRenamed, "OldFolderName");
        var deltaItems = new List<DeltaItem> { renamedFolder };
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "next-token", false);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Ok>();

        await _fileTransferService.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_delta_contains_folder_renamed_item_then_files_downloaded_count_is_zero()
    {
        const string accountId = "account-folder-rename-count";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var renamedFolder = DeltaItemFactory.Create("folder-id", "NewName", null, DeltaItemType.FolderRenamed, "OldName");
        var deltaItems = new List<DeltaItem> { renamedFolder };
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "next-token", false);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        var result = await BuildEngine().StartSyncAsync(accountId, ct: ct);

        var report = ((Result<SyncReport, SyncEngineError>.Ok)result).Value;
        report.FilesDownloaded.ShouldBe(0);
    }

    public void Dispose() => _gate.Dispose();
}
