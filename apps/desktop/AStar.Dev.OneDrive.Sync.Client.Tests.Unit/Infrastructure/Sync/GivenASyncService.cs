using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncService
{
    private readonly IAuthService          _authService          = Substitute.For<IAuthService>();
    private readonly ISyncRepository       _syncRepository       = Substitute.For<ISyncRepository>();
    private readonly ISyncPassOrchestrator _syncPassOrchestrator = Substitute.For<ISyncPassOrchestrator>();
    private readonly IConflictApplier      _conflictApplier      = Substitute.For<IConflictApplier>();

    private SyncService BuildSut()
        => new(_authService, _syncRepository, _syncPassOrchestrator, _conflictApplier, Substitute.For<ILogger<SyncService>>(), Substitute.For<ILocalizationService>());

    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var service = BuildSut();

        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_sync_called_with_valid_account_then_auth_service_is_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id         = new AccountId("user-1"),
            Profile    = AccountProfileFactory.Create("User", "user@outlook.com"),
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/home/user/OneDrive"))
        };

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_auth_succeeds_and_sync_config_is_set_then_orchestrator_is_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id         = new AccountId("user-1"),
            Profile    = AccountProfileFactory.Create("User", "user@outlook.com"),
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/home/user/OneDrive"))
        };

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        await _syncPassOrchestrator.Received(1).OrchestrateAsync(
            Arg.Is(account),
            Arg.Any<string>(),
            Arg.Any<Func<SyncConflict, Task>>(),
            Arg.Any<Action<SyncProgressEventArgs>>(),
            Arg.Any<Action<JobCompletedEventArgs>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_auth_fails_then_error_progress_is_raised()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Authentication failed"));

        var service = BuildSut();
        var account = new OneDriveAccount { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com") };
        bool errorRaised = false;
        service.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
                errorRaised = true;
        };

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        errorRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task when_sync_config_is_none_then_error_progress_is_raised()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

        var service = BuildSut();
        var account = new OneDriveAccount { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"), SyncConfig = Option.None<AccountSyncConfig>() };
        bool noSyncPathRaised = false;
        service.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
                noSyncPathRaised = true;
        };

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        noSyncPathRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task when_sync_called_then_sync_progress_changed_event_is_raised()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("fail"));

        var service = BuildSut();
        var account = new OneDriveAccount { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com") };
        bool eventRaised = false;
        service.SyncProgressChanged += (_, _) => eventRaised = true;

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task when_resolve_conflict_called_with_valid_policy_then_sync_repository_resolve_is_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var service = BuildSut();
        var conflict = new SyncConflict
        {
            Id       = Guid.NewGuid(),
            Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
            Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow.AddMinutes(-5), 0L),
            State    = ConflictState.Pending
        };

        await service.ResolveConflictAsync(conflict, ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task when_resolve_conflict_auth_fails_then_sync_repository_resolve_is_not_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Auth failed"));

        var service = BuildSut();
        var conflict = new SyncConflict { Id = Guid.NewGuid(), Remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)) };

        await service.ResolveConflictAsync(conflict, ConflictPolicy.Ignore, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public async Task when_resolve_conflict_called_with_various_policies_then_policy_is_recorded(ConflictPolicy policy)
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var service = BuildSut();
        var conflict = new SyncConflict { Id = Guid.NewGuid(), Remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)) };

        await service.ResolveConflictAsync(conflict, policy, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Is(policy));
    }
}
