using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Options;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassOrchestrator
{
    private readonly IAccountRepository     _accountRepository     = Substitute.For<IAccountRepository>();
    private readonly IDriveStateRepository  _driveStateRepository  = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector _remoteDeletionDetector = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalDeletionDetector  _localDeletionDetector  = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalChangeDetector    _localChangeDetector    = Substitute.For<ILocalChangeDetector>();
    private readonly ISyncJobExecutor        _syncJobExecutor        = Substitute.For<ISyncJobExecutor>();
    private readonly IDownloadJobBuilder     _downloadJobBuilder     = Substitute.For<IDownloadJobBuilder>();
    private readonly ILocalizationService    _localizationService    = Substitute.For<ILocalizationService>();

    public GivenASyncPassOrchestrator()
    {
        _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
    }

    private static IOptions<SyncSettings> SyncSettingsOptions
        => Options.Create(new SyncSettings { ProgressReportInterval = 100 });

    private ISyncPassOrchestrator CreateSut()
    {
        var dependencies = new SyncServiceDependencies(
            _remoteFolderEnumerator,
            _remoteDeletionDetector,
            _localDeletionDetector,
            _localChangeDetector,
            _syncJobExecutor,
            _downloadJobBuilder);

        return new SyncPassOrchestrator(_accountRepository, _driveStateRepository, dependencies, SyncSettingsOptions, _localizationService);
    }

    private static OneDriveAccount CreateAccount(string localSyncPath = "/path/to/sync") => new()
    {
        Id                = new AccountId("user-1"),
        Profile           = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig        = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(localSyncPath)),
        SelectedFolderIds = []
    };

    private static RemoteEnumerationResult EmptyEnumerationResult() => new([], new HashSet<string>(), [], []);

    private static bool IsEnumerationProgressEvent(SyncProgressEventArgs args)
        => args.CurrentFile.StartsWith("Sync.Enumerating", StringComparison.Ordinal);

    private void SetupDeepSyncPrerequisites()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEnumerationResult());
        _downloadJobBuilder.BuildAsync(Arg.Any<OneDriveAccount>(), Arg.Any<IReadOnlyList<DeltaItem>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<Dictionary<string, SyncedItemEntity>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SyncJob>)[]);
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>())
            .Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<AccountEntity>());
    }

    private void SetupEnumeratorWithCallbacks(int callbackCount)
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var onItemDiscovered = callInfo.ArgAt<Action<int>?>(2);
                for (var i = 1; i <= callbackCount; i++)
                    onItemDiscovered?.Invoke(i);
                return Task.FromResult(EmptyEnumerationResult());
            });
        _downloadJobBuilder.BuildAsync(Arg.Any<OneDriveAccount>(), Arg.Any<IReadOnlyList<DeltaItem>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<Dictionary<string, SyncedItemEntity>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SyncJob>)[]);
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>())
            .Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<AccountEntity>());
    }

    [Fact]
    public async Task when_orchestrate_is_called_then_remote_folder_enumerator_is_invoked()
    {
        SetupDeepSyncPrerequisites();

        var sut     = CreateSut();
        var account = CreateAccount();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");

        await sut.OrchestrateAsync(account, tokenFactory, _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _remoteFolderEnumerator.Received(1).EnumerateAsync(
            Arg.Is(account),
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<Action<int>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_returns_had_no_rules_then_orchestrate_returns_false()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new RemoteEnumerationResult([], new HashSet<string>(), [], [], HadNoRules: true));

        var sut     = CreateSut();
        var account = CreateAccount();

        bool result = await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task when_enumeration_returns_had_no_rules_then_remote_deletion_detector_not_called()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new RemoteEnumerationResult([], new HashSet<string>(), [], [], HadNoRules: true));

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _remoteDeletionDetector.DidNotReceive().DetectAndApplyAsync(
            Arg.Any<AccountId>(),
            Arg.Any<Dictionary<string, SyncedItemEntity>>(),
            Arg.Any<IReadOnlySet<string>>(),
            Arg.Any<IReadOnlyList<SyncRuleEntity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_succeeds_then_orchestrate_returns_true()
    {
        SetupDeepSyncPrerequisites();

        var sut     = CreateSut();
        var account = CreateAccount();

        bool result = await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task when_enumeration_succeeds_then_remote_deletion_detector_is_called()
    {
        SetupDeepSyncPrerequisites();

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _remoteDeletionDetector.Received(1).DetectAndApplyAsync(
            Arg.Any<AccountId>(),
            Arg.Any<Dictionary<string, SyncedItemEntity>>(),
            Arg.Any<IReadOnlySet<string>>(),
            Arg.Any<IReadOnlyList<SyncRuleEntity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_succeeds_then_local_deletion_detector_is_called()
    {
        SetupDeepSyncPrerequisites();

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _localDeletionDetector.Received(1).DetectAndApplyAsync(
            Arg.Any<AccountId>(),
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<Dictionary<string, SyncedItemEntity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_jobs_exist_then_job_executor_is_not_called()
    {
        SetupDeepSyncPrerequisites();

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _syncJobExecutor.DidNotReceive().ExecuteAsync(
            Arg.Any<OneDriveAccount>(),
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<IReadOnlyList<SyncJob>>(),
            Arg.Any<Dictionary<string, SyncedItemEntity>>(),
            Arg.Any<Action<SyncProgressEventArgs>>(),
            Arg.Any<Action<JobCompletedEventArgs>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_jobs_exist_then_no_changes_progress_is_raised()
    {
        SetupDeepSyncPrerequisites();

        var progressMessages = new List<string>();
        var sut              = CreateSut();
        var account          = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), ct: TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.NoChanges");
    }

    [Fact]
    public async Task when_enumeration_succeeds_then_progress_includes_detecting_remote_deletions_before_local_changes()
    {
        SetupDeepSyncPrerequisites();

        var progressMessages = new List<string>();
        var sut              = CreateSut();
        var account          = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), ct: TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.DetectingRemoteDeletions");
        progressMessages.ShouldContain("Sync.DetectingLocalChanges");
        progressMessages.IndexOf("Sync.DetectingRemoteDeletions").ShouldBeLessThan(progressMessages.IndexOf("Sync.DetectingLocalChanges"));
    }

    [Fact]
    public async Task when_account_entity_exists_then_account_repository_upsert_is_called()
    {
        SetupDeepSyncPrerequisites();
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.Some(new AccountEntity { Id = new AccountId("user-1") }));

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _accountRepository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_entity_does_not_exist_then_account_repository_upsert_is_not_called()
    {
        SetupDeepSyncPrerequisites();

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _accountRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_conflict_is_detected_then_conflict_callback_is_invoked()
    {
        SetupDeepSyncPrerequisites();

        var conflict = new SyncConflict
        {
            Id     = Guid.NewGuid(),
            Remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty))
        };
        bool callbackInvoked = false;

        _downloadJobBuilder.BuildAsync(Arg.Any<OneDriveAccount>(), Arg.Any<IReadOnlyList<DeltaItem>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<Dictionary<string, SyncedItemEntity>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(async args =>
            {
                var callback = args.ArgAt<Func<SyncConflict, Task>>(4);
                await callback(conflict);

                return (IReadOnlyList<SyncJob>)[];
            });

        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), async detectedConflict =>
        {
            if(detectedConflict.Id == conflict.Id)
                callbackInvoked = true;
            await Task.CompletedTask;
        }, ct: TestContext.Current.CancellationToken);

        callbackInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_enumeration_fires_99_item_discovered_callbacks_then_no_enumeration_progress_events_are_raised()
    {
        SetupEnumeratorWithCallbacks(99);

        var enumerationProgressEvents = new List<SyncProgressEventArgs>();
        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask,
            onProgress: args =>
            {
                if (IsEnumerationProgressEvent(args))
                    enumerationProgressEvents.Add(args);
            },
            ct: TestContext.Current.CancellationToken);

        enumerationProgressEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_enumeration_fires_100_item_discovered_callbacks_then_one_enumeration_progress_event_is_raised()
    {
        SetupEnumeratorWithCallbacks(100);

        var enumerationProgressEvents = new List<SyncProgressEventArgs>();
        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask,
            onProgress: args =>
            {
                if (IsEnumerationProgressEvent(args))
                    enumerationProgressEvents.Add(args);
            },
            ct: TestContext.Current.CancellationToken);

        enumerationProgressEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_enumeration_fires_200_item_discovered_callbacks_then_two_enumeration_progress_events_are_raised()
    {
        SetupEnumeratorWithCallbacks(200);

        var enumerationProgressEvents = new List<SyncProgressEventArgs>();
        var sut     = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, _ => Task.FromResult("token"), _ => Task.CompletedTask,
            onProgress: args =>
            {
                if (IsEnumerationProgressEvent(args))
                    enumerationProgressEvents.Add(args);
            },
            ct: TestContext.Current.CancellationToken);

        enumerationProgressEvents.Count.ShouldBe(2);
    }
}
