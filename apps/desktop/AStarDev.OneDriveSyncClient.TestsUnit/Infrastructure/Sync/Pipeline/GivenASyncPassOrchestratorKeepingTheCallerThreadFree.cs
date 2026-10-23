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

public sealed class GivenASyncPassOrchestratorKeepingTheCallerThreadFree
{
    private readonly IDriveStateRepository driveStateRepository = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly ILocalDeletionDetector localDeletionDetector = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();
    private readonly ISettingsService settingsService = Substitute.For<ISettingsService>();
    private readonly IFileClassificationRepository classificationRepository = Substitute.For<IFileClassificationRepository>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalChangeDetector localChangeDetector = Substitute.For<ILocalChangeDetector>();
    private int localDeletionDetectionThreadId;

    public GivenASyncPassOrchestratorKeepingTheCallerThreadFree()
    {
        localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        settingsService.Current.Returns(new AppSettings { ConcurrentWorkerCount = 4 });
        classificationRepository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<DriveStateEntity>());
        remoteFolderEnumerator.StreamAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<RemoteEnumerationContext>(), Arg.Any<Action<string, int>?>(), Arg.Any<Action<string>?>(), Arg.Any<CancellationToken>()).Returns(EmptyStream());
        localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>()).Returns([]);
        accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<AccountEntity>());
        localDeletionDetector.DetectAndApplyAsync(Arg.Any<AccountId>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<System.Collections.Concurrent.ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                localDeletionDetectionThreadId = Environment.CurrentManagedThreadId;

                return Task.FromResult<IReadOnlyList<SyncedItemEntity>>([]);
            });
    }

    private static async IAsyncEnumerable<DeltaItem> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private SyncPassOrchestrator CreateSut()
    {
        var dependencies = new SyncServiceDependencies(remoteFolderEnumerator, Substitute.For<IRemoteDeletionDetector>(), localDeletionDetector, localChangeDetector, Substitute.For<ISyncJobExecutor>(), Substitute.For<IDownloadJobBuilder>(), Substitute.For<IRemoteFolderCreator>());
        var syncPassRepositories = new SyncPassRepositories(accountRepository, driveStateRepository, classificationRepository);

        return new SyncPassOrchestrator(syncPassRepositories, dependencies, Options.Create(new SyncSettings { ProgressReportInterval = 100 }), settingsService, localizationService, new TestLogger<SyncPassOrchestrator>());
    }

    [Fact]
    public async Task when_the_calling_thread_starts_a_pass_then_local_deletion_detection_does_not_run_on_it()
    {
        var sut = CreateSut();
        var syncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync"));
        var account = new OneDriveAccount
        {
            Id = new AccountId("user-1"),
            Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
            SyncConfig = syncConfig,
            SelectedFolderIds = []
        };
        int callerThreadId = 0;

        await Task.Factory.StartNew(
            () =>
            {
                callerThreadId = Environment.CurrentManagedThreadId;
                sut.OrchestrateAsync(account, syncConfig, _ => Task.FromResult("token"), _ => Task.CompletedTask, cancellationToken: TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            },
            TestContext.Current.CancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        localDeletionDetectionThreadId.ShouldNotBe(callerThreadId);
    }
}
