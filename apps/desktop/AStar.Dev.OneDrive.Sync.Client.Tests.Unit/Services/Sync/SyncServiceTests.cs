using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public class SyncServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();

        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public async Task SyncAccountAsync_WithValidAccount_ShouldCompleteSuccessfully()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var account = new OneDriveAccount
        {
            Id = "user-1",
            Email = "user@outlook.com",
            LocalSyncPath = "/home/user/OneDrive",
            SelectedFolderIds = []
        };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        _ = await mockAuthService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAccountAsync_WhenAuthFails_ShouldRaiseErrorEvent()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var account = new OneDriveAccount { Id = "user-1", Email = "user@outlook.com" };
        bool errorRaised = false;

        service.SyncProgressChanged += (s, args) =>
        {
            if(args.SyncState == SyncState.Error)
                errorRaised = true;
        };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Failure("Authentication failed"));

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        errorRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task SyncAccountAsync_WithoutSyncPath_ShouldRaiseNoSyncPathEvent()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var account = new OneDriveAccount
        {
            Id = "user-1",
            Email = "user@outlook.com",
            LocalSyncPath = null!
        };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

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
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var account = new OneDriveAccount
        {
            Id = "user-1",
            Email = "user@outlook.com",
            LocalSyncPath = "/path/to/sync",
            SelectedFolderIds = []
        };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        bool eventRaised = false;
        service.SyncProgressChanged += (s, args) => eventRaised = true;

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveConflictAsync_WithValidPolicy_ShouldResolveConflict()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var conflict = new SyncConflict
        {
            Id = Guid.NewGuid(),
            AccountId = "user-1",
            LocalModified = DateTimeOffset.UtcNow,
            RemoteModified = DateTimeOffset.UtcNow.AddMinutes(-5),
            State = ConflictState.Pending
        };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.ResolveConflictAsync(conflict, ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        await mockSyncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Fact]
    public async Task ResolveConflictAsync_WhenAuthFails_ShouldNotResolve()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Failure("Auth failed"));

        await service.ResolveConflictAsync(conflict, ConflictPolicy.Ignore, TestContext.Current.CancellationToken);
        await mockSyncRepository.DidNotReceive().ResolveConflictAsync(Arg.Any<Guid>(), Arg.Any<ConflictPolicy>());
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public async Task ResolveConflictAsync_WithVariousPolicies_ShouldApply(ConflictPolicy policy)
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.ResolveConflictAsync(conflict, policy, TestContext.Current.CancellationToken);

        await mockSyncRepository.Received(1).ResolveConflictAsync(Arg.Any<Guid>(), Arg.Is(policy));
    }

    [Fact]
    public async Task SyncAccountAsync_WithMultipleFolders_ShouldSyncAll()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var account = new OneDriveAccount
        {
            Id = "user-1",
            Email = "user@outlook.com",
            LocalSyncPath = "/path/to/sync",
            SelectedFolderIds = ["folder-1", "folder-2", "folder-3"]
        };

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.SyncAccountAsync(account, TestContext.Current.CancellationToken);

        // Service should attempt to sync each folder
    }

    [Fact]
    public async Task SyncAccountAsync_ShouldAcceptCancellationToken()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var account = new OneDriveAccount
        {
            Id = "user-1",
            Email = "user@outlook.com",
            DisplayName = "Test User",
            LocalSyncPath = "/path/to/sync"
        };

        var cts = new CancellationTokenSource();

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.SyncAccountAsync(account, cts.Token);
    }

    [Fact]
    public async Task ResolveConflictAsync_ShouldAcceptCancellationToken()
    {
        var mockAuthService = Substitute.For<IAuthService>();
        var mockGraphService = Substitute.For<IGraphService>();
        var mockAccountRepository = Substitute.For<IAccountRepository>();
        var mockSyncRepository = Substitute.For<ISyncRepository>();
        var service = new SyncService(mockAuthService, mockGraphService, mockAccountRepository, mockSyncRepository);

        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };
        var cts = new CancellationTokenSource();

        _ = mockAuthService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

        await service.ResolveConflictAsync(conflict, ConflictPolicy.Ignore, cts.Token);
    }
}
