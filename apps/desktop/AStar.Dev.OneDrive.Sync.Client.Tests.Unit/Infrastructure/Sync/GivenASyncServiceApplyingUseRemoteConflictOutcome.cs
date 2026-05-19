using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceApplyingUseRemoteConflictOutcome
{
    private readonly IAuthService     _authService     = Substitute.For<IAuthService>();
    private readonly ISyncRepository  _syncRepository  = Substitute.For<ISyncRepository>();
    private readonly IConflictApplier _conflictApplier = Substitute.For<IConflictApplier>();

    public GivenASyncServiceApplyingUseRemoteConflictOutcome()
        => _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

    private SyncService CreateSut()
        => new(_authService, _syncRepository, Substitute.For<ISyncPassOrchestrator>(), _conflictApplier, Substitute.For<ILogger<SyncService>>());

    private static SyncConflict CreateConflict() => new()
    {
        Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1")),
        Target   = SyncFileTargetFactory.Create("/local/path/file.txt", "file.txt"),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddMinutes(-5), 0L, DateTimeOffset.UtcNow, 0L)
    };

    [Fact]
    public async Task when_applier_returns_false_then_error_progress_is_raised()
    {
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

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
    public async Task when_applier_returns_false_then_sync_repository_resolve_is_not_called()
    {
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await CreateSut().ResolveConflictAsync(CreateConflict(), ConflictPolicy.RemoteWins, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task when_applier_returns_true_then_sync_repository_resolve_is_called()
    {
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await CreateSut().ResolveConflictAsync(CreateConflict(), ConflictPolicy.RemoteWins, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }
}
