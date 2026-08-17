using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync;

public sealed class GivenASyncServiceLocalisingAuthenticating
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ISyncPassOrchestrator _syncPassOrchestrator = Substitute.For<ISyncPassOrchestrator>();
    private readonly IConflictApplier _conflictApplier = Substitute.For<IConflictApplier>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    public GivenASyncServiceLocalisingAuthenticating()
        => _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

    private SyncService CreateSut()
        => new(_authService, _syncRepository, _syncPassOrchestrator, _conflictApplier, Substitute.For<ILogger<SyncService>>(), _localizationService);

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync")),
        SelectedFolderIds = []
    };

    [Fact]
    public async Task when_sync_starts_then_localisation_service_GetLocal_is_called_with_Sync_Authenticating()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("fail"));

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.Authenticating");
    }

    [Fact]
    public async Task when_sync_starts_then_authenticating_progress_uses_localisation_key()
    {
        var authCallOrder = new List<string>();
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                authCallOrder.Add("auth");

                return Task.FromResult(AuthResultFactory.Failure("fail"));
            });

        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if (args.CurrentFile == "Sync.Authenticating")
                authCallOrder.Add("progress");
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        authCallOrder.ShouldBe(["progress", "auth"]);
    }
}
