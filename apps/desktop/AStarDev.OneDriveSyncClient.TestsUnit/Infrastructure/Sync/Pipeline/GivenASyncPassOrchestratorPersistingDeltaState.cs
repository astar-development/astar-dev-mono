using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassOrchestratorPersistingDeltaState
{
    private const string StoredLink = "stored-link";
    private const string NewLink = "new-link";

    private static readonly AccountId UserOne = new("user-1");
    private static readonly IReadOnlyList<SyncRuleEntity> Rules = [new SyncRuleEntity { AccountId = UserOne, RemotePath = "/A", RuleType = RuleType.Include, RemoteItemId = Option.Some("rule-root") }];

    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IDriveStateRepository _driveStateRepository = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector _remoteDeletionDetector = Substitute.For<IRemoteDeletionDetector>();
    private readonly ISyncJobExecutor _syncJobExecutor = Substitute.For<ISyncJobExecutor>();
    private readonly ILocalChangeDetector _localChangeDetector = Substitute.For<ILocalChangeDetector>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IFileClassificationRepository _classificationRepository = Substitute.For<IFileClassificationRepository>();
    private readonly List<DriveStateEntity> _upserts = [];

    public GivenASyncPassOrchestratorPersistingDeltaState()
    {
        _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        _settingsService.Current.Returns(new AppSettings { ConcurrentWorkerCount = 4 });
        _classificationRepository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.Some(new DriveStateEntity { AccountId = UserOne, DeltaLink = Option.Some(StoredLink), RulesFingerprint = Option.Some("old-fingerprint") }));
        _driveStateRepository.When(repository => repository.UpsertAsync(Arg.Any<DriveStateEntity>(), Arg.Any<CancellationToken>())).Do(call => _upserts.Add(Snapshot(call.Arg<DriveStateEntity>())));
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>()).Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<AccountEntity>());
    }

    private static DriveStateEntity Snapshot(DriveStateEntity state)
        => new() { AccountId = state.AccountId, DeltaLink = state.DeltaLink, RulesFingerprint = state.RulesFingerprint, LastFullEnumerationAt = state.LastFullEnumerationAt, LastSyncStartedAt = state.LastSyncStartedAt };

    private static async IAsyncEnumerable<DeltaItem> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private void GivenEnumeratorRecords(RemoteWalkDecision decision, bool enumerationFailed = false)
        => _remoteFolderEnumerator.StreamAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<RemoteEnumerationContext>(), Arg.Any<Action<string, int>?>(), Arg.Any<Action<string>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var context = call.Arg<RemoteEnumerationContext>();
                context.Rules = Rules;
                context.WalkDecision = Option.Some(decision);
                context.HadEnumerationFailures = enumerationFailed;

                return EmptyStream();
            });

    private async Task<SyncPassResult> OrchestrateAsync(int failedJobs = 0)
    {
        var syncJobExecutor = _syncJobExecutor;
        _ = syncJobExecutor.ExecuteAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<System.Collections.Concurrent.ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<CancellationToken>()).Returns(failedJobs);

        var dependencies = new SyncServiceDependencies(_remoteFolderEnumerator, _remoteDeletionDetector, Substitute.For<ILocalDeletionDetector>(), _localChangeDetector, _syncJobExecutor, Substitute.For<IDownloadJobBuilder>(), Substitute.For<IRemoteFolderCreator>());
        var repositories = new SyncPassRepositories(_accountRepository, _driveStateRepository, _classificationRepository);
        var sut = new SyncPassOrchestrator(repositories, dependencies, Options.Create(new SyncSettings { ProgressReportInterval = 100 }), _settingsService, _localizationService, NullLogger<SyncPassOrchestrator>.Instance);
        var syncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/sync-root"));
        var account = new OneDriveAccount
        {
            Id = UserOne,
            Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
            SyncConfig = syncConfig,
            SelectedFolderIds = []
        };

        return await sut.OrchestrateAsync(account, syncConfig, _ => Task.FromResult("token"), _ => Task.CompletedTask, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_a_pass_starts_then_the_stored_delta_link_is_not_cleared()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateSkip(NewLink), enumerationFailed: true);

        _ = await OrchestrateAsync();

        _upserts.First().DeltaLink.ShouldBe(Option.Some(StoredLink));
    }

    [Fact]
    public async Task when_a_walk_completes_cleanly_then_the_new_delta_link_fingerprint_and_enumeration_time_are_stored()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.RulesChanged, Option.Some(NewLink)));

        _ = await OrchestrateAsync();

        var stored = _upserts.Last();
        stored.DeltaLink.ShouldBe(Option.Some(NewLink));
        stored.RulesFingerprint.ShouldBe(Option.Some(RulesFingerprintCalculator.Compute(Rules)));
        _ = stored.LastFullEnumerationAt.ShouldBeOfType<Option<DateTimeOffset>.Some>();
    }

    [Fact]
    public async Task when_a_walk_completes_cleanly_without_a_new_delta_link_then_the_stored_delta_link_is_cleared()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.NoDeltaLink, Option.None<string>()));

        _ = await OrchestrateAsync();

        _upserts.Last().DeltaLink.ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_walk_is_skipped_then_only_the_delta_link_advances()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateSkip(NewLink));

        _ = await OrchestrateAsync();

        var stored = _upserts.Last();
        stored.DeltaLink.ShouldBe(Option.Some(NewLink));
        stored.RulesFingerprint.ShouldBe(Option.Some("old-fingerprint"));
        stored.LastFullEnumerationAt.ShouldBe(Option.None<DateTimeOffset>());
    }

    [Fact]
    public async Task when_the_walk_is_skipped_then_remote_deletion_detection_is_not_run()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateSkip(NewLink));

        _ = await OrchestrateAsync();

        await _remoteDeletionDetector.DidNotReceive().DetectAndApplyAsync(Arg.Any<AccountId>(), Arg.Any<System.Collections.Concurrent.ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_walk_is_required_then_remote_deletion_detection_is_run()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.RulesChanged, Option.Some(NewLink)));

        _ = await OrchestrateAsync();

        await _remoteDeletionDetector.Received(1).DetectAndApplyAsync(Arg.Any<AccountId>(), Arg.Any<System.Collections.Concurrent.ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_had_failures_then_the_delta_state_is_not_advanced()
    {
        GivenEnumeratorRecords(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.RulesChanged, Option.Some(NewLink)), enumerationFailed: true);

        _ = await OrchestrateAsync();

        _upserts.ShouldAllBe(state => state.DeltaLink == Option.Some(StoredLink));
    }
}
