using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncServiceSyncingAnAccount
{
    private readonly IAuthService              _authService              = Substitute.For<IAuthService>();
    private readonly IAccountRepository        _accountRepository        = Substitute.For<IAccountRepository>();
    private readonly ISyncRepository           _syncRepository           = Substitute.For<ISyncRepository>();
    private readonly IDriveStateRepository     _driveStateRepository     = Substitute.For<IDriveStateRepository>();
    private readonly IHttpDownloader           _httpDownloader           = Substitute.For<IHttpDownloader>();
    private readonly IGraphService             _graphService             = Substitute.For<IGraphService>();
    private readonly IRemoteFolderEnumerator   _remoteFolderEnumerator   = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector   _remoteDeletionDetector   = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalDeletionDetector    _localDeletionDetector    = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalChangeDetector      _localChangeDetector      = Substitute.For<ILocalChangeDetector>();
    private readonly ISyncJobExecutor          _syncJobExecutor          = Substitute.For<ISyncJobExecutor>();
    private readonly IFileSystem               _fileSystem               = Substitute.For<IFileSystem>();

    private SyncService CreateSut()
    {
        var dependencies = new SyncServiceDependencies(
            _remoteFolderEnumerator,
            _remoteDeletionDetector,
            _localDeletionDetector,
            _localChangeDetector,
            _syncJobExecutor);

        return new SyncService(_authService, _accountRepository, _driveStateRepository, _syncRepository, _httpDownloader, _graphService, dependencies, _fileSystem);
    }

    private static OneDriveAccount CreateAccount(string localSyncPath = "/path/to/sync") => new()
    {
        Id                = new AccountId("user-1"),
        Profile           = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig        = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(localSyncPath)),
        SelectedFolderIds = []
    };

    private static RemoteEnumerationResult EmptyEnumerationResult()
        => new([], new HashSet<string>(), [], []);

    private void SetupAuthSuccess() =>
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "user-1", AccountProfileFactory.Create("User", "user@outlook.com")));

    private void SetupDeepSyncPrerequisites()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEnumerationResult());
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>())
            .Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<AccountEntity>());
    }

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
            .Returns(AuthResultFactory.Failure("fail"));

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

    [Fact]
    public async Task when_auth_returns_cancelled_result_then_progress_message_is_auth_failed()
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

        capturedMessage.ShouldBe("Auth failed");
    }

    [Fact]
    public async Task when_auth_fails_with_error_message_then_that_message_appears_in_progress_with_error_state()
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

        capturedMessage.ShouldBe("Custom error message");
        capturedState.ShouldBe(SyncState.Error);
    }

    [Fact]
    public async Task when_enumerator_throws_operation_cancelled_then_progress_is_sync_cancelled_with_idle_state()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RemoteEnumerationResult>(new OperationCanceledException()));

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Sync cancelled")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync cancelled");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_enumerator_throws_unexpected_exception_then_progress_is_error_state_with_exception_message()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RemoteEnumerationResult>(new InvalidOperationException("unexpected")));

        SyncState? capturedState = null;
        string? capturedMessage = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.SyncState == SyncState.Error && args.CurrentFile != "Authenticating...")
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
    public async Task when_enumerator_returns_had_no_rules_then_progress_raises_no_folders_selected_with_idle_state()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(new RemoteEnumerationResult([], new HashSet<string>(), [], [], HadNoRules: true));

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "No folders selected")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("No folders selected");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_no_download_or_upload_jobs_exist_then_progress_raises_no_changes_with_idle_state()
    {
        SetupDeepSyncPrerequisites();

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "No changes")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("No changes");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_sync_completes_with_no_jobs_then_progress_raises_sync_complete_with_idle_state()
    {
        SetupDeepSyncPrerequisites();

        string? capturedMessage = null;
        SyncState? capturedState = null;
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) =>
        {
            if(args.CurrentFile == "Sync complete")
            {
                capturedMessage = args.CurrentFile;
                capturedState = args.SyncState;
            }
        };

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        capturedMessage.ShouldBe("Sync complete");
        capturedState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_rules_exist_then_progress_sequence_includes_detecting_remote_deletions_and_detecting_local_changes()
    {
        SetupDeepSyncPrerequisites();

        var progressMessages = new List<string>();
        var sut = CreateSut();
        sut.SyncProgressChanged += (_, args) => progressMessages.Add(args.CurrentFile);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Detecting remote deletions...");
        progressMessages.ShouldContain("Detecting local changes...");
        progressMessages.IndexOf("Detecting remote deletions...").ShouldBeLessThan(progressMessages.IndexOf("Detecting local changes..."));
    }

    [Fact]
    public async Task when_account_entity_exists_then_account_repository_upsert_is_called_after_sync()
    {
        SetupDeepSyncPrerequisites();
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.Some(new AccountEntity { Id = new AccountId("user-1") }));

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _accountRepository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_entity_does_not_exist_then_account_repository_upsert_is_not_called()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _accountRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }
}
