using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenASyncServiceSyncingAnAccount
{
    private readonly IAuthService              _authService              = Substitute.For<IAuthService>();
    private readonly IGraphService             _graphService             = Substitute.For<IGraphService>();
    private readonly IAccountRepository        _accountRepository        = Substitute.For<IAccountRepository>();
    private readonly ISyncRepository           _syncRepository           = Substitute.For<ISyncRepository>();
    private readonly IDriveStateRepository     _driveStateRepository     = Substitute.For<IDriveStateRepository>();
    private readonly ISyncRuleRepository       _syncRuleRepository       = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncedItemRepository     _syncedItemRepository     = Substitute.For<ISyncedItemRepository>();
    private readonly ILocalChangeDetector      _localChangeDetector      = Substitute.For<ILocalChangeDetector>();
    private readonly IHttpDownloader           _httpDownloader           = Substitute.For<IHttpDownloader>();
    private readonly IParallelDownloadPipeline _parallelDownloadPipeline = Substitute.For<IParallelDownloadPipeline>();

    private SyncService CreateSut() => new(_authService, _graphService, _accountRepository, _syncRepository, _driveStateRepository, _syncRuleRepository, _syncedItemRepository, _localChangeDetector, _httpDownloader, _parallelDownloadPipeline);

    private static OneDriveAccount CreateAccount(string localSyncPath = "/path/to/sync") => new()
    {
        Id = new AccountId("user-1"),
        Email = "user@outlook.com",
        LocalSyncPath = LocalSyncPath.Restore(localSyncPath),
        SelectedFolderIds = []
    };

    private static SyncRuleEntity MakeIncludeRule(string remotePath = "/Documents", string? remoteItemId = null) => new() { RemotePath = remotePath, RuleType = RuleType.Include, RemoteItemId = remoteItemId };

    private void SetupAuthSuccess() =>
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("token", "user-1", "User", "user@outlook.com"));

    private void SetupDeepSyncPrerequisites()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((DriveStateEntity?)null);
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity>());
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("drive-1");
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>())
            .Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((AccountEntity?)null);
    }

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

    [Fact]
    public async Task when_auth_fails_with_null_error_message_then_progress_message_is_auth_failed()
    {
        var authResult = new AuthResult(false, false, null, null, null, null, null);
        _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(authResult);

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
            .Returns(AuthResult.Failure("Custom error message"));

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
    public async Task when_internal_sync_throws_operation_cancelled_then_progress_is_sync_cancelled_with_idle_state()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((DriveStateEntity?)null);
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<SyncRuleEntity>>(new OperationCanceledException()));

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
    public async Task when_internal_sync_throws_unexpected_exception_then_progress_is_error_state_with_exception_message()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((DriveStateEntity?)null);
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<SyncRuleEntity>>(new InvalidOperationException("unexpected")));

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
    public async Task when_no_sync_rules_are_configured_then_progress_raises_no_folders_selected_with_idle_state()
    {
        SetupAuthSuccess();
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns((DriveStateEntity?)null);
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

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
    public async Task when_rule_has_no_remote_item_id_and_folder_id_cannot_be_resolved_then_enumerate_folder_is_not_called()
    {
        SetupDeepSyncPrerequisites();
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: null)]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_rule_has_no_remote_item_id_and_folder_id_is_resolved_then_sync_rule_repository_upsert_is_called_to_back_fill_id()
    {
        SetupDeepSyncPrerequisites();
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: null)]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("folder-1");
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _syncRuleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Is(RuleType.Include), Arg.Is("folder-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_rule_already_has_remote_item_id_matching_resolved_folder_then_sync_rule_repository_upsert_is_not_called()
    {
        SetupDeepSyncPrerequisites();
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_download_or_upload_jobs_exist_then_progress_raises_no_changes_with_idle_state()
    {
        SetupDeepSyncPrerequisites();
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());

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
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());

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
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());

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
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new AccountEntity { Id = new AccountId("user-1") });

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _accountRepository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_entity_does_not_exist_then_account_repository_upsert_is_not_called()
    {
        SetupDeepSyncPrerequisites();
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([MakeIncludeRule(remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeltaItem>());

        var sut = CreateSut();

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);

        await _accountRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }
}
