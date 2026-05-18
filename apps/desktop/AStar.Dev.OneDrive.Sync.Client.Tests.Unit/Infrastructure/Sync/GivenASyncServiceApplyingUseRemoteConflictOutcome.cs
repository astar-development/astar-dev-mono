using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;
using ReactiveUnit = global::System.Reactive.Unit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceApplyingUseRemoteConflictOutcome
{
    private readonly IAuthService    _authService    = Substitute.For<IAuthService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly IHttpDownloader _httpDownloader = Substitute.For<IHttpDownloader>();
    private readonly IGraphService   _graphService   = Substitute.For<IGraphService>();

    public GivenASyncServiceApplyingUseRemoteConflictOutcome()
        => _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

    private SyncService CreateSut()
        => new(_authService, _syncRepository, _httpDownloader, _graphService, Substitute.For<ISyncPassOrchestrator>(), Substitute.For<IFileSystem>());

    private static SyncConflict CreateConflict() => new()
    {
        Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1")),
        Target   = SyncFileTargetFactory.Create("/local/path/file.txt", "file.txt"),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddMinutes(-5), 0L, DateTimeOffset.UtcNow, 0L)
    };

    [Fact]
    public async Task when_url_resolution_fails_then_error_progress_is_raised()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Error("url-resolution-failed"));

        SyncProgressEventArgs? captured = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
                captured = args;
        };

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.RemoteWins, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.SyncState.ShouldBe(SyncState.Error);
    }

    [Fact]
    public async Task when_download_fails_then_error_progress_is_raised()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok("https://example.com/file.txt"));
        _httpDownloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ReactiveUnit, string>.Error("download-failed"));

        SyncProgressEventArgs? captured = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
                captured = args;
        };

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.RemoteWins, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.SyncState.ShouldBe(SyncState.Error);
    }

    [Fact]
    public async Task when_download_fails_then_sync_repository_resolve_is_not_called()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok("https://example.com/file.txt"));
        _httpDownloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ReactiveUnit, string>.Error("download-failed"));

        await CreateSut().ResolveConflictAsync(CreateConflict(), ConflictPolicy.RemoteWins, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }
}
