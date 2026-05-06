using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class SyncServiceTests
{
    private readonly IAuthService              _authService             = Substitute.For<IAuthService>();
    private readonly IGraphService             _graphService            = Substitute.For<IGraphService>();
    private readonly IAccountRepository        _accountRepository       = Substitute.For<IAccountRepository>();
    private readonly ISyncRepository           _syncRepository          = Substitute.For<ISyncRepository>();
    private readonly IDriveStateRepository     _driveStateRepository    = Substitute.For<IDriveStateRepository>();
    private readonly IHttpDownloader           _httpDownloader          = Substitute.For<IHttpDownloader>();
    private readonly IRemoteFolderEnumerator   _remoteFolderEnumerator  = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector   _remoteDeletionDetector  = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalDeletionDetector    _localDeletionDetector   = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalChangeDetector      _localChangeDetector     = Substitute.For<ILocalChangeDetector>();
    private readonly ISyncJobExecutor          _syncJobExecutor         = Substitute.For<ISyncJobExecutor>();
    private readonly IFileSystem               _fileSystem              = Substitute.For<IFileSystem>();

    private SyncService BuildSut()
    {
        var dependencies = new SyncServiceDependencies(
            _remoteFolderEnumerator,
            _remoteDeletionDetector,
            _localDeletionDetector,
            _localChangeDetector,
            _syncJobExecutor);

        return new SyncService(_authService, _accountRepository, _driveStateRepository, _syncRepository, _httpDownloader, _graphService, dependencies, _fileSystem);
    }

    private static RemoteEnumerationResult EmptyEnumerationResult()
        => new([], new HashSet<string>(), [], []);

    [Fact]
    public void constructor_creates_instance_successfully()
    {
        var service = BuildSut();

        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_sync_called_with_valid_account_then_auth_service_is_called()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEnumerationResult());
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>())
            .Returns([]);

        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id         = new AccountId("user-1"),
            Profile    = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/home/user/OneDrive"))
        };

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
    public async Task when_sync_config_is_null_then_no_local_sync_path_progress_is_raised()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

        var service = BuildSut();
        var account = new OneDriveAccount { Id = new AccountId("user-1"), Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"), SyncConfig = null };
        bool noSyncPathRaised = false;
        service.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "No local sync path configured")
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

        var service = BuildSut();
        var conflict = new SyncConflict
        {
            Id             = Guid.NewGuid(),
            AccountId      = "user-1",
            LocalModified  = DateTimeOffset.UtcNow,
            RemoteModified = DateTimeOffset.UtcNow.AddMinutes(-5),
            State          = ConflictState.Pending
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
        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };

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

        var service = BuildSut();
        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };

        await service.ResolveConflictAsync(conflict, policy, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Is(policy));
    }
}
