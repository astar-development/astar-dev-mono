using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncPassOrchestrator
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IDriveStateRepository _driveStateRepository = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector _remoteDeletionDetector = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalDeletionDetector _localDeletionDetector = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalChangeDetector _localChangeDetector = Substitute.For<ILocalChangeDetector>();
    private readonly ISyncJobExecutor _syncJobExecutor = Substitute.For<ISyncJobExecutor>();

    private SyncPassOrchestrator CreateSut()
    {
        var dependencies = new SyncServiceDependencies(
            _remoteFolderEnumerator,
            _remoteDeletionDetector,
            _localDeletionDetector,
            _localChangeDetector,
            _syncJobExecutor);

        return new SyncPassOrchestrator(_accountRepository, _driveStateRepository, dependencies);
    }

    private static OneDriveAccount CreateAccount(string localSyncPath = "/path/to/sync") => new()
    {
        Id = new AccountId("user-1"),
        Email = "user@outlook.com",
        LocalSyncPath = LocalSyncPath.Restore(localSyncPath),
        SelectedFolderIds = []
    };

    private static RemoteEnumerationResult EmptyEnumerationResult() => new([], new HashSet<string>(), [], []);

    private void SetupDeepSyncPrerequisites()
    {
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
    public async Task when_orchestrate_is_called_then_remote_folder_enumerator_is_invoked()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();
        var account = CreateAccount();
        var token = "token";

        await sut.OrchestrateAsync(account, token, _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _remoteFolderEnumerator.Received(1).EnumerateAsync(
            Arg.Is(account),
            Arg.Is(token),
            Arg.Any<Func<SyncConflict, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_returns_had_no_rules_then_orchestrate_returns_false()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(new RemoteEnumerationResult([], new HashSet<string>(), [], [], HadNoRules: true));

        var sut = CreateSut();
        var account = CreateAccount();

        var result = await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task when_enumeration_returns_had_no_rules_then_remote_deletion_detector_not_called()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(new RemoteEnumerationResult([], new HashSet<string>(), [], [], HadNoRules: true));

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

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

        var sut = CreateSut();
        var account = CreateAccount();

        var result = await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task when_enumeration_succeeds_then_remote_deletion_detector_is_called()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

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

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _localDeletionDetector.Received(1).DetectAndApplyAsync(
            Arg.Any<AccountId>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, SyncedItemEntity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_jobs_exist_then_job_executor_is_not_called()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _syncJobExecutor.DidNotReceive().ExecuteAsync(
            Arg.Any<OneDriveAccount>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<SyncJob>>(),
            Arg.Any<Dictionary<string, SyncedItemEntity>>(),
            Arg.Any<Action<SyncProgressEventArgs>>(),
            Arg.Any<Action<JobCompletedEventArgs>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_entity_exists_then_account_repository_upsert_is_called()
    {
        SetupDeepSyncPrerequisites();
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.Some(new AccountEntity { Id = new AccountId("user-1") }));

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _accountRepository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_account_entity_does_not_exist_then_account_repository_upsert_is_not_called()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        await _accountRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_conflict_is_detected_then_conflict_callback_is_invoked()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());

        var conflict = new SyncConflict { Id = Guid.NewGuid(), AccountId = "user-1" };
        var callbackInvoked = false;

        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<string>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns(async args =>
            {
                var callback = args.ArgAt<Func<SyncConflict, Task>>(2);
                await callback(conflict);
                return new RemoteEnumerationResult([], new HashSet<string>(), [], [], HadNoRules: true);
            });

        var sut = CreateSut();
        var account = CreateAccount();

        await sut.OrchestrateAsync(account, "token", async detectedConflict =>
        {
            if (detectedConflict.Id == conflict.Id)
                callbackInvoked = true;
            await Task.CompletedTask;
        }, ct: TestContext.Current.CancellationToken);

        callbackInvoked.ShouldBeTrue();
    }
}
