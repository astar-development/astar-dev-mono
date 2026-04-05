using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using AStar.Dev.Sync.Engine.Features.Activity;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.Activity;

public sealed class GivenSyncEngineActivityReporting : IDisposable
{
    private const string AccountId = "account-activity-test";
    private static readonly string[] LocalFiles = ["local-file.txt"];

    private readonly SyncGate _gate = new();
    private readonly ISyncStateStore _stateStore = Substitute.For<ISyncStateStore>();
    private readonly ISyncProgressReporter _progressReporter = Substitute.For<ISyncProgressReporter>();
    private readonly IActivityReporter _activityReporter = Substitute.For<IActivityReporter>();
    private readonly IDeltaQueryService _deltaQueryService = Substitute.For<IDeltaQueryService>();
    private readonly IFileTransferService _fileTransferService = Substitute.For<IFileTransferService>();
    private readonly ILocalFileScanner _localFileScanner = Substitute.For<ILocalFileScanner>();
    private readonly IDiskSpaceChecker _diskSpaceChecker = Substitute.For<IDiskSpaceChecker>();
    private readonly IDbBackupService _dbBackupService = Substitute.For<IDbBackupService>();

    public GivenSyncEngineActivityReporting()
    {
        _dbBackupService.BackupAsync(Arg.Any<CancellationToken>()).Returns(true);
        _stateStore.GetStateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((SyncAccountState?)null);
        _diskSpaceChecker.GetAvailableFreeSpace(Arg.Any<string>()).Returns(long.MaxValue);
        _localFileScanner.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AsyncEnumerable.Empty<string>());
    }

    [Fact]
    public async Task when_file_is_downloaded_then_activity_reporter_receives_downloaded_event()
    {
        var ct = TestContext.Current.CancellationToken;
        SetupSuccessfulDownload("downloaded.txt");

        await BuildEngine().StartSyncAsync(AccountId, ct: ct);

        _activityReporter.Received(1).Report(AccountId, ActivityActionType.Downloaded, Arg.Any<string>());
    }

    [Fact]
    public async Task when_file_download_fails_then_activity_reporter_receives_error_event()
    {
        var ct = TestContext.Current.CancellationToken;
        SetupFailedDownload("failed-download.txt");

        await BuildEngine().StartSyncAsync(AccountId, ct: ct);

        _activityReporter.Received(1).Report(AccountId, ActivityActionType.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task when_file_is_uploaded_then_activity_reporter_receives_uploaded_event()
    {
        var ct = TestContext.Current.CancellationToken;

        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create([], "token", false)));

        _localFileScanner.ScanAsync(AccountId, Arg.Any<CancellationToken>()).Returns(LocalFiles.ToAsyncEnumerable());
        _fileTransferService.UploadAsync(Arg.Any<string>(), "local-file.txt", AccountId, Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<OneDrive.Client.Features.FileOperations.FileUploadResult, string>.Ok(new("remote-id", "local-file.txt", 0)));

        await BuildEngine().StartSyncAsync(AccountId, ct: ct);

        _activityReporter.Received(1).Report(AccountId, ActivityActionType.Uploaded, Arg.Any<string>());
    }

    [Fact]
    public async Task when_file_upload_fails_then_activity_reporter_receives_error_event()
    {
        var ct = TestContext.Current.CancellationToken;

        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create([], "token", false)));

        _localFileScanner.ScanAsync(AccountId, Arg.Any<CancellationToken>()).Returns(LocalFiles.ToAsyncEnumerable());
        _fileTransferService.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<OneDrive.Client.Features.FileOperations.FileUploadResult, string>.Error("upload error"));

        await BuildEngine().StartSyncAsync(AccountId, ct: ct);

        _activityReporter.Received(1).Report(AccountId, ActivityActionType.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task when_file_is_skipped_during_full_resync_then_activity_reporter_receives_skipped_event()
    {
        const string localFilePath = "/sync/matching.txt";
        const long fileSize = 1_024L;
        var remoteLastModified = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var ct = TestContext.Current.CancellationToken;

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [localFilePath] = new MockFileData(new byte[fileSize]) { LastWriteTime = remoteLastModified.UtcDateTime }
        });

        var matchingFile = DeltaItemFactory.Create("file-id-1", localFilePath, null, DeltaItemType.File, size: fileSize, lastModifiedDateTime: remoteLastModified);
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create([matchingFile], "token", true)));

        await BuildEngine(fileSystem).StartSyncAsync(AccountId, isFullResync: true, ct: ct);

        _activityReporter.Received(1).Report(AccountId, ActivityActionType.Skipped, Arg.Any<string>());
    }

    public void Dispose() => _gate.Dispose();

    private SyncEngine BuildEngine(MockFileSystem? fileSystem = null) =>
        new(_gate, _stateStore, _progressReporter, _activityReporter, _deltaQueryService, _fileTransferService, _localFileScanner, _diskSpaceChecker, _dbBackupService, new ExponentialBackoffPolicy(NullLogger<ExponentialBackoffPolicy>.Instance), fileSystem ?? new MockFileSystem(), Substitute.For<IConflictDetector>(), Substitute.For<IConflictStore>(), NullLogger<SyncEngine>.Instance);

    private void SetupSuccessfulDownload(string fileName)
    {
        var deltaItems = new List<DeltaItem> { DeltaItemFactory.Create("file-id", fileName, null, DeltaItemType.File) };
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(deltaItems, "token", false)));
        _fileTransferService.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<OneDrive.Client.Features.FileOperations.FileDownloadResult, string>.Ok(new(fileName, 0)));
    }

    private void SetupFailedDownload(string fileName)
    {
        var deltaItems = new List<DeltaItem> { DeltaItemFactory.Create("file-id", fileName, null, DeltaItemType.File) };
        _deltaQueryService.GetDeltaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DeltaQueryResult, DeltaQueryError>.Ok(DeltaQueryResultFactory.Create(deltaItems, "token", false)));
        _fileTransferService.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<OneDrive.Client.Features.FileOperations.FileDownloadResult, string>.Error("download error"));
    }
}
