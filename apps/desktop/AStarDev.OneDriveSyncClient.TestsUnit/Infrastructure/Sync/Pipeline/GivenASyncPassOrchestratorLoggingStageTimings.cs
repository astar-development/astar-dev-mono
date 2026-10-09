using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassOrchestratorLoggingStageTimings
{
    private const int StageTimingEventId = 2009;
    private const int ExpectedStageCount = 9;

    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IDriveStateRepository _driveStateRepository = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly ILocalChangeDetector _localChangeDetector = Substitute.For<ILocalChangeDetector>();
    private readonly IDownloadJobBuilder _downloadJobBuilder = Substitute.For<IDownloadJobBuilder>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IFileClassificationRepository _classificationRepository = Substitute.For<IFileClassificationRepository>();
    private readonly TestLogger<SyncPassOrchestrator> _logger = new();

    public GivenASyncPassOrchestratorLoggingStageTimings()
    {
        _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        _settingsService.Current.Returns(new AppSettings { ConcurrentWorkerCount = 4 });
        _classificationRepository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.StreamAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<RemoteEnumerationContext>(), Arg.Any<Action<string, int>?>(), Arg.Any<Action<string>?>(), Arg.Any<CancellationToken>()).Returns(EmptyStream());
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>()).Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<AccountEntity>());
    }

    private static async IAsyncEnumerable<DeltaItem> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private SyncPassOrchestrator CreateSut()
    {
        var dependencies = new SyncServiceDependencies(_remoteFolderEnumerator, Substitute.For<IRemoteDeletionDetector>(), Substitute.For<ILocalDeletionDetector>(), _localChangeDetector, Substitute.For<ISyncJobExecutor>(), _downloadJobBuilder, Substitute.For<IRemoteFolderCreator>());
        var syncPassRepositories = new SyncPassRepositories(_accountRepository, _driveStateRepository, _classificationRepository);

        return new SyncPassOrchestrator(syncPassRepositories, dependencies, Options.Create(new SyncSettings { ProgressReportInterval = 100 }), _settingsService, _localizationService, _logger);
    }

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync")),
        SelectedFolderIds = []
    };

    [Fact]
    public async Task when_a_pass_with_no_jobs_completes_then_a_timing_is_logged_for_every_stage()
    {
        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync")), _ => Task.FromResult("token"), _ => Task.CompletedTask, cancellationToken: TestContext.Current.CancellationToken);

        _logger.Entries.Count(entry => entry.EventId.Id == StageTimingEventId).ShouldBe(ExpectedStageCount);
    }
}
