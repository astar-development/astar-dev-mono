using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceLocalisingStrings
{
    private readonly IAuthService           _authService           = Substitute.For<IAuthService>();
    private readonly ISyncRepository        _syncRepository        = Substitute.For<ISyncRepository>();
    private readonly ISyncPassOrchestrator  _syncPassOrchestrator  = Substitute.For<ISyncPassOrchestrator>();
    private readonly IConflictApplier       _conflictApplier       = Substitute.For<IConflictApplier>();
    private readonly ILocalizationService   _localizationService   = Substitute.For<ILocalizationService>();

    public GivenASyncServiceLocalisingStrings()
        => _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

    private SyncService CreateSut()
        => new(_authService, _syncRepository, _syncPassOrchestrator, _conflictApplier, Substitute.For<ILogger<SyncService>>(), _localizationService);

    private static OneDriveAccount CreateAccount(bool withSyncConfig = true) => new()
    {
        Id                = new AccountId("user-1"),
        Profile           = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig        = withSyncConfig ? AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync")) : Option.None<AccountSyncConfig>(),
        SelectedFolderIds = []
    };

    private static SyncConflict CreateConflict() => new()
    {
        Id       = Guid.NewGuid(),
        Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow.AddMinutes(-5), 0L),
        State    = ConflictState.Pending
    };

    [Fact]
    public async Task when_auth_fails_with_auth_failed_error_then_localisation_key_Sync_AuthFailed_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("some error"));
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.AuthFailed");
    }

    [Fact]
    public async Task when_auth_returns_cancelled_then_localisation_key_Sync_AuthFailed_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.AuthFailed");
    }

    [Fact]
    public async Task when_sync_config_is_null_then_localisation_key_Sync_NoSyncPath_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(withSyncConfig: false), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.NoSyncPath");
    }

    [Fact]
    public async Task when_orchestrator_returns_false_then_localisation_key_Sync_NoFoldersSelected_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.NoFoldersSelected");
    }

    [Fact]
    public async Task when_orchestrator_returns_true_then_localisation_key_Sync_Complete_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.Complete");
    }

    [Fact]
    public async Task when_orchestrator_throws_operation_cancelled_then_localisation_key_Sync_Cancelled_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new OperationCanceledException()));
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.Cancelled");
    }

    [Fact]
    public async Task when_conflict_applier_returns_false_then_localisation_key_Sync_ConflictResolutionFailed_is_used()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.ConflictResolutionFailed");
    }

    [Fact]
    public async Task when_auth_fails_then_localisation_service_GetLocal_is_called_with_Sync_AuthFailed()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("some error"));

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.AuthFailed");
    }

    [Fact]
    public async Task when_sync_config_is_null_then_localisation_service_GetLocal_is_called_with_Sync_NoSyncPath()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(withSyncConfig: false), TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.NoSyncPath");
    }

    [Fact]
    public async Task when_orchestrator_returns_false_then_localisation_service_GetLocal_is_called_with_Sync_NoFoldersSelected()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.NoFoldersSelected");
    }

    [Fact]
    public async Task when_orchestrator_returns_true_then_localisation_service_GetLocal_is_called_with_Sync_Complete()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.Complete");
    }

    [Fact]
    public async Task when_orchestrator_throws_operation_cancelled_then_localisation_service_GetLocal_is_called_with_Sync_Cancelled()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new OperationCanceledException()));

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.Cancelled");
    }

    [Fact]
    public async Task when_conflict_applier_returns_false_then_localisation_service_GetLocal_is_called_with_Sync_ConflictResolutionFailed()
    {
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));
        _conflictApplier.ApplyAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictOutcome>(), Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        await sut.ResolveConflictAsync(CreateConflict(), ConflictPolicy.LastWriteWins, TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.ConflictResolutionFailed");
    }
}
