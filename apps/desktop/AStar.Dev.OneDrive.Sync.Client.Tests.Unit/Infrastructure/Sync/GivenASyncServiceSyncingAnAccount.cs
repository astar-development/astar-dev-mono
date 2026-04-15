using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenASyncServiceSyncingAnAccount
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ILocalChangeDetector _localChangeDetector = Substitute.For<ILocalChangeDetector>();
    private readonly IHttpDownloader _httpDownloader = Substitute.For<IHttpDownloader>();
    private readonly IParallelDownloadPipeline _parallelDownloadPipeline = Substitute.For<IParallelDownloadPipeline>();

    private SyncService CreateSut() => new(_authService, _graphService, _accountRepository, _syncRepository, _localChangeDetector, _httpDownloader, _parallelDownloadPipeline);

    private static OneDriveAccount CreateAccount(string localSyncPath = "/path/to/sync") => new()
    {
        Id = new AccountId("user-1"),
        Email = "user@outlook.com",
        LocalSyncPath = LocalSyncPath.Restore(localSyncPath),
        SelectedFolderIds = []
    };

    [Fact]
    public async Task when_sync_starts_then_authenticating_progress_is_raised_before_auth_call()
    {
        var authCallOrder = new List<string>();

        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                authCallOrder.Add("auth");

                return Task.FromResult(AuthResult.Failure("fail"));
            });

        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Authenticating...")
                authCallOrder.Add("progress");
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        authCallOrder.ShouldBe(["progress", "auth"]);
    }

    [Fact]
    public async Task when_sync_starts_then_authenticating_progress_has_syncing_state()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Failure("fail"));

        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Authenticating...")
                capturedState = args.SyncState;
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedState.ShouldBe(SyncState.Syncing);
    }
}
