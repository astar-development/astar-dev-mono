using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceSyncingAnAccount
{
    private readonly IAuthService          _authService          = Substitute.For<IAuthService>();
    private readonly ISyncRepository       _syncRepository       = Substitute.For<ISyncRepository>();
    private readonly ISyncPassOrchestrator _syncPassOrchestrator = Substitute.For<ISyncPassOrchestrator>();
    private readonly IConflictApplier      _conflictApplier      = Substitute.For<IConflictApplier>();
    private readonly ILocalizationService  _localizationService  = Substitute.For<ILocalizationService>();

    public GivenASyncServiceSyncingAnAccount()
        => _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

    private SyncService CreateSut()
        => new(_authService, _syncRepository, _syncPassOrchestrator, _conflictApplier, Substitute.For<ILogger<SyncService>>(), _localizationService);

    private static OneDriveAccount CreateAccount(string localSyncPath = "/path/to/sync") => new()
    {
        Id                = new AccountId("user-1"),
        Profile           = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig        = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(localSyncPath)),
        SelectedFolderIds = []
    };

    private void SetupAuthSuccess() =>
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

    private void SetupOrchestratorReturns(bool didRun) =>
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<AccountSyncConfig>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(didRun);

    [Fact]
    public async Task when_sync_starts_then_authenticating_progress_is_raised_before_auth_call()
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
            if(args.CurrentFile == "Sync.Authenticating")
                authCallOrder.Add("progress");
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        authCallOrder.ShouldBe(["progress", "auth"]);
    }

    [Fact]
    public async Task when_sync_starts_then_authenticating_progress_has_syncing_state()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("fail"));

        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Sync.Authenticating")
                capturedState = args.SyncState;
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_auth_returns_cancelled_result_then_progress_message_is_auth_failed_localisation_key()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());

        string? capturedMessage = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
                capturedMessage = args.CurrentFile;
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync.AuthFailed");
    }

    [Fact]
    public async Task when_auth_fails_then_progress_message_is_auth_failed_localisation_key_with_error_state()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Custom error message"));

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error)
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync.AuthFailed");
        capturedState.ShouldBe(SyncState.Error);
    }

    [Fact]
    public async Task when_orchestrator_throws_operation_cancelled_then_progress_is_sync_cancelled_localisation_key_with_idle_state()
    {
        SetupAuthSuccess();
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<AccountSyncConfig>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new OperationCanceledException()));

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Sync.Cancelled")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync.Cancelled");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_orchestrator_throws_unexpected_exception_then_progress_is_error_state_with_exception_message()
    {
        SetupAuthSuccess();
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<AccountSyncConfig>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("unexpected")));

        SyncState? capturedState = null;
        string? capturedMessage = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error && args.CurrentFile != "Sync.Authenticating")
            {
                capturedState = args.SyncState;
                capturedMessage = args.CurrentFile;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedState.ShouldBe(SyncState.Error);
        capturedMessage.ShouldBe("unexpected");
    }

    [Fact]
    public async Task when_orchestrator_returns_false_then_no_folders_selected_localisation_key_is_raised_with_idle_state()
    {
        SetupAuthSuccess();
        SetupOrchestratorReturns(false);

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Sync.NoFoldersSelected")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync.NoFoldersSelected");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_orchestrator_returns_true_then_sync_complete_localisation_key_is_raised_with_idle_state()
    {
        SetupAuthSuccess();
        SetupOrchestratorReturns(true);

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Sync.Complete")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync.Complete");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_sync_completes_then_acquire_token_silent_is_called_exactly_once()
    {
        SetupAuthSuccess();
        SetupOrchestratorReturns(true);

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sync_starts_then_token_factory_returns_the_initial_access_token()
    {
        SetupAuthSuccess();

        Func<CancellationToken, Task<string>>? capturedFactory = null;
        _syncPassOrchestrator.OrchestrateAsync(
            Arg.Any<OneDriveAccount>(),
            Arg.Any<AccountSyncConfig>(),
            Arg.Do<Func<CancellationToken, Task<string>>>(f => capturedFactory = f),
            Arg.Any<Func<SyncConflict, Task>>(),
            Arg.Any<Action<SyncProgressEventArgs>>(),
            Arg.Any<Action<JobCompletedEventArgs>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        var token = await capturedFactory!(TestContext.Current.CancellationToken);

        token.ShouldBe("token");
    }

    [Fact]
    public async Task when_token_factory_is_invoked_multiple_times_then_acquire_token_silent_is_not_called_again()
    {
        SetupAuthSuccess();

        Func<CancellationToken, Task<string>>? capturedFactory = null;
        _syncPassOrchestrator.OrchestrateAsync(
            Arg.Any<OneDriveAccount>(),
            Arg.Any<AccountSyncConfig>(),
            Arg.Do<Func<CancellationToken, Task<string>>>(f => capturedFactory = f),
            Arg.Any<Func<SyncConflict, Task>>(),
            Arg.Any<Action<SyncProgressEventArgs>>(),
            Arg.Any<Action<JobCompletedEventArgs>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);
        await capturedFactory!(TestContext.Current.CancellationToken);
        await capturedFactory!(TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
