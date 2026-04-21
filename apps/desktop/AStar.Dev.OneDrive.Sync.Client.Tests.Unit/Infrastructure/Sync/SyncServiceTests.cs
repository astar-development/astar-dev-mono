using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class SyncServiceTests
{
    private readonly IAuthService             authService             = Substitute.For<IAuthService>();
    private readonly IGraphService            graphService            = Substitute.For<IGraphService>();
    private readonly IAccountRepository       accountRepository       = Substitute.For<IAccountRepository>();
    private readonly ISyncRepository          syncRepository          = Substitute.For<ISyncRepository>();
    private readonly IDriveStateRepository    driveStateRepository    = Substitute.For<IDriveStateRepository>();
    private readonly ISyncRuleRepository      syncRuleRepository      = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncedItemRepository    syncedItemRepository    = Substitute.For<ISyncedItemRepository>();
    private readonly ILocalChangeDetector     localChangeDetector     = Substitute.For<ILocalChangeDetector>();
    private readonly IHttpDownloader          httpDownloader          = Substitute.For<IHttpDownloader>();
    private readonly IParallelDownloadPipeline parallelDownloadPipeline = Substitute.For<IParallelDownloadPipeline>();

    private SyncService BuildSut() => new(authService, graphService, accountRepository, syncRepository, driveStateRepository, syncRuleRepository, syncedItemRepository, localChangeDetector, httpDownloader, parallelDownloadPipeline);

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        var service = BuildSut();

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public async Task SyncAccountAsync_WithValidAccount_ShouldCompleteSuccessfully()
    {
        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id            = new AccountId("user-1"),
            Email         = "user@outlook.com",
            LocalSyncPath = LocalSyncPath.Restore("/home/user/OneDrive")
        };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity>());

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        _ = await authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAccountAsync_WhenAuthFails_ShouldRaiseErrorEvent()
    {
        var service = BuildSut();
        var account = new OneDriveAccount { Id = new AccountId("user-1"), Email = "user@outlook.com" };
        bool errorRaised = false;

        service.SyncProgressChanged += (s, args) =>
        {
            if(args.SyncState == SyncState.Error)
                errorRaised = true;
        };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Failure("Authentication failed"));

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        errorRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task SyncAccountAsync_WithoutSyncPath_ShouldRaiseNoSyncPathEvent()
    {
        var service = BuildSut();
        var account = new OneDriveAccount { Id = new AccountId("user-1"), Email = "user@outlook.com", LocalSyncPath = null };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        bool noSyncPathRaised = false;
        service.SyncProgressChanged += (s, args) =>
        {
            if(args.CurrentFile == "No local sync path configured")
                noSyncPathRaised = true;
        };

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        noSyncPathRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task SyncAccountAsync_RaisesSyncProgressChangedEvent()
    {
        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id            = new AccountId("user-1"),
            Email         = "user@outlook.com",
            LocalSyncPath = LocalSyncPath.Restore("/path/to/sync")
        };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity>());

        bool eventRaised = false;
        service.SyncProgressChanged += (s, args) => eventRaised = true;

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveConflictAsync_WithValidPolicy_ShouldResolveConflict()
    {
        var service = BuildSut();
        var conflict = new SyncConflict
        {
            Id             = Guid.NewGuid(),
            AccountId      = "user-1",
            LocalModified  = DateTimeOffset.UtcNow,
            RemoteModified = DateTimeOffset.UtcNow.AddMinutes(-5),
            State          = ConflictState.Pending
        };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.ResolveConflictAsync(conflict, ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task ResolveConflictAsync_WhenAuthFails_ShouldNotResolve()
    {
        var service = BuildSut();
        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Failure("Auth failed"));

        await service.ResolveConflictAsync(conflict, ConflictPolicy.Ignore, TestContext.Current.CancellationToken);
        await syncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public async Task ResolveConflictAsync_WithVariousPolicies_ShouldApply(ConflictPolicy policy)
    {
        var service = BuildSut();
        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.ResolveConflictAsync(conflict, policy, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Is(policy));
    }

    [Fact]
    public async Task SyncAccountAsync_WithMultipleFolders_ShouldSyncAll()
    {
        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id            = new AccountId("user-1"),
            Email         = "user@outlook.com",
            LocalSyncPath = LocalSyncPath.Restore("/path/to/sync")
        };

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity>());

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SyncAccountAsync_ShouldAcceptCancellationToken()
    {
        var service = BuildSut();
        var account = new OneDriveAccount
        {
            Id            = new AccountId("user-1"),
            Email         = "user@outlook.com",
            DisplayName   = "Test User",
            LocalSyncPath = LocalSyncPath.Restore("/path/to/sync")
        };
        var cts = new CancellationTokenSource();

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity>());

        await service.SyncAccountAsync(account, cts.Token);
    }

    [Fact]
    public async Task ResolveConflictAsync_ShouldAcceptCancellationToken()
    {
        var service = BuildSut();
        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };
        var cts = new CancellationTokenSource();

        _ = authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.ResolveConflictAsync(conflict, ConflictPolicy.Ignore, cts.Token);
    }
}
