using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceResolvingConflicts
{
    private readonly IAuthService          _authService          = Substitute.For<IAuthService>();
    private readonly ISyncRepository       _syncRepository       = Substitute.For<ISyncRepository>();
    private readonly ISyncPassOrchestrator _syncPassOrchestrator = Substitute.For<ISyncPassOrchestrator>();
    private readonly IConflictApplier      _conflictApplier      = Substitute.For<IConflictApplier>();

    private SyncService CreateSut()
        => new(_authService, _syncRepository, _syncPassOrchestrator, _conflictApplier, Substitute.For<ILogger<SyncService>>());

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
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
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

    [Fact]
    public async Task when_conflict_applier_returns_false_then_sync_repository_resolve_is_not_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = CreateSut();

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task when_conflict_applier_returns_false_then_error_progress_is_raised()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        SyncProgressEventArgs? captured = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
                captured = args;
        };

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.SyncState.ShouldBe(SyncState.Error);
    }

    [Fact]
    public async Task when_auth_succeeds_and_applier_succeeds_then_conflict_resolved_event_is_raised()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var conflict = CreateConflict();
        SyncConflict? resolved = null;
        var sut = CreateSut();
        sut.ConflictResolved += (_, c) => resolved = c;

        await sut.ResolveConflictAsync(conflict, ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        resolved.ShouldNotBeNull();
        resolved.Id.ShouldBe(conflict.Id);
    }
}
