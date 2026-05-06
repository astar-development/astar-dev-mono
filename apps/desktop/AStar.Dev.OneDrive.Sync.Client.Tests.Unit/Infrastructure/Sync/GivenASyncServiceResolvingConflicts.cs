using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceResolvingConflicts
{
    private readonly IAuthService            _authService         = Substitute.For<IAuthService>();
    private readonly ISyncRepository         _syncRepository      = Substitute.For<ISyncRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector _remoteDeletionDetector = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalDeletionDetector  _localDeletionDetector  = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalChangeDetector    _localChangeDetector    = Substitute.For<ILocalChangeDetector>();
    private readonly ISyncJobExecutor        _syncJobExecutor        = Substitute.For<ISyncJobExecutor>();

    private SyncService CreateSut()
    {
        var dependencies = new SyncServiceDependencies(
            _remoteFolderEnumerator,
            _remoteDeletionDetector,
            _localDeletionDetector,
            _localChangeDetector,
            _syncJobExecutor);

        return new SyncService(
            _authService,
            Substitute.For<IAccountRepository>(),
            Substitute.For<IDriveStateRepository>(),
            _syncRepository,
            Substitute.For<IHttpDownloader>(),
            Substitute.For<IGraphService>(),
            dependencies,
            Substitute.For<IFileSystem>());
    }

    private static SyncConflict CreateConflict() => new()
    {
        Id       = Guid.NewGuid(),
        Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow.AddMinutes(-5), 0L),
        State    = ConflictState.Pending
    };

    [Fact]
    public async Task when_auth_succeeds_then_sync_repository_resolve_is_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        var sut = CreateSut();

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task when_auth_fails_with_auth_failed_error_then_sync_repository_resolve_is_not_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token refresh failed"));
        var sut = CreateSut();

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task when_auth_is_cancelled_then_sync_repository_resolve_is_not_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = CreateSut();

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }
}
