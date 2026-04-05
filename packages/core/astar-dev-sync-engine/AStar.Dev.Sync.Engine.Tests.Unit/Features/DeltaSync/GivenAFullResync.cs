using System.IO.Abstractions.TestingHelpers;
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
using AStar.Dev.Sync.Engine.Features.Activity;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.DeltaSync;

public sealed class GivenAFullResync : IDisposable
{
    private const string LocalFilePath = "/sync/matching.txt";
    private const long FileSize = 1_024L;

    private static readonly DateTimeOffset RemoteLastModified = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SyncGate _gate = new();
    private readonly ISyncStateStore _stateStore = Substitute.For<ISyncStateStore>();
    private readonly ISyncProgressReporter _progressReporter = Substitute.For<ISyncProgressReporter>();
    private readonly IDeltaQueryService _deltaQueryService = Substitute.For<IDeltaQueryService>();
    private readonly IFileTransferService _fileTransferService = Substitute.For<IFileTransferService>();
    private readonly ILocalFileScanner _localFileScanner = Substitute.For<ILocalFileScanner>();
    private readonly IDiskSpaceChecker _diskSpaceChecker = Substitute.For<IDiskSpaceChecker>();
    private readonly IDbBackupService _dbBackupService = Substitute.For<IDbBackupService>();

    private readonly MockFileSystem _fileSystem = new(new Dictionary<string, MockFileData>
    {
        [LocalFilePath] = new MockFileData(new byte[FileSize]) { LastWriteTime = RemoteLastModified.UtcDateTime },
    });

    private SyncEngine BuildEngine() => new(_gate, _stateStore, _progressReporter, Substitute.For<IActivityReporter>(), _deltaQueryService, _fileTransferService, _localFileScanner, _diskSpaceChecker, _dbBackupService, new ExponentialBackoffPolicy(NullLogger<ExponentialBackoffPolicy>.Instance), _fileSystem, Substitute.For<IConflictDetector>(), Substitute.For<IConflictStore>(), NullLogger<SyncEngine>.Instance);

    [Fact]
    public async Task when_full_resync_and_file_matches_local_size_and_timestamp_then_file_is_skipped()
    {
        const string accountId = "account-full-resync";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var matchingFile = DeltaItemFactory.Create("file-id-1", LocalFilePath, null, DeltaItemType.File, size: FileSize, lastModifiedDateTime: RemoteLastModified);
        var deltaItems = new List<DeltaItem> { matchingFile };
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "next-token", true);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        var result = await BuildEngine().StartSyncAsync(accountId, isFullResync: true, ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Ok>();
        var report = ((Result<SyncReport, SyncEngineError>.Ok)result).Value;
        report.FilesSkipped.ShouldBe(1);
        report.FilesDownloaded.ShouldBe(0);

        await _fileTransferService.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_full_resync_and_remote_file_has_no_metadata_then_file_is_downloaded()
    {
        const string accountId = "account-full-resync-no-meta";
        var ct = TestContext.Current.CancellationToken;

        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(accountId, Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());

        var noMetaFile = DeltaItemFactory.Create("file-id-2", "no-meta.txt", null, DeltaItemType.File);
        var deltaItems = new List<DeltaItem> { noMetaFile };
        var deltaResult = DeltaQueryResultFactory.Create(deltaItems, "next-token", true);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(deltaResult));

        _fileTransferService.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<OneDrive.Client.Features.FileOperations.FileDownloadResult, string>.Ok(new("no-meta.txt", 0)));

        var result = await BuildEngine().StartSyncAsync(accountId, isFullResync: true, ct);

        result.ShouldBeOfType<Result<SyncReport, SyncEngineError>.Ok>();
        var report = ((Result<SyncReport, SyncEngineError>.Ok)result).Value;
        report.FilesDownloaded.ShouldBe(1);
        report.FilesSkipped.ShouldBe(0);
    }

    public void Dispose() => _gate.Dispose();
}
