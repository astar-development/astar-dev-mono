using System.Collections.Concurrent;
using AStar.Dev.Infrastructure.AppDb.Domain;
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
using Microsoft.Extensions.Logging.Abstractions;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassOrchestratorReportingRemoteDeletions
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IDriveStateRepository _driveStateRepository = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector _remoteDeletionDetector = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalChangeDetector _localChangeDetector = Substitute.For<ILocalChangeDetector>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IFileClassificationRepository _classificationRepository = Substitute.For<IFileClassificationRepository>();

    public GivenASyncPassOrchestratorReportingRemoteDeletions()
    {
        _localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        _settingsService.Current.Returns(new AppSettings { ConcurrentWorkerCount = 4 });
        _classificationRepository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.StreamAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<RemoteEnumerationContext>(), Arg.Any<Action<string, int>?>(), Arg.Any<Action<string>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyStream());
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>()).Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Option.None<AccountEntity>());
    }

    private static async IAsyncEnumerable<DeltaItem> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static SyncedItemEntity DeletedRemoteFolder()
        => new() { AccountId = new AccountId("user-1"), RemoteItemId = new OneDriveItemId("folder-1"), RemotePath = "/Documents/Old", LocalPath = "/path/to/sync/Documents/Old", IsFolder = true };

    private void SetupRemoteDeletions(params SyncedItemEntity[] deleted)
        => _remoteDeletionDetector.DetectAndApplyAsync(Arg.Any<AccountId>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncedItemEntity>>(deleted));

    private ISyncPassOrchestrator CreateSut()
    {
        var dependencies = new SyncServiceDependencies(_remoteFolderEnumerator, _remoteDeletionDetector, Substitute.For<ILocalDeletionDetector>(), _localChangeDetector, Substitute.For<ISyncJobExecutor>(), Substitute.For<IDownloadJobBuilder>(), Substitute.For<IRemoteFolderCreator>());
        var syncPassRepositories = new SyncPassRepositories(_accountRepository, _driveStateRepository, _classificationRepository);

        return new SyncPassOrchestrator(syncPassRepositories, dependencies, Options.Create(new SyncSettings { ProgressReportInterval = 100 }), _settingsService, _localizationService, NullLogger<SyncPassOrchestrator>.Instance);
    }

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync")),
        SelectedFolderIds = []
    };

    private static AccountSyncConfig CreateSyncConfig() => AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync"));

    [Fact]
    public async Task when_a_remote_folder_deletion_is_applied_locally_then_a_completed_delete_job_is_reported()
    {
        SetupRemoteDeletions(DeletedRemoteFolder());
        var completedJobs = new List<SyncJob>();

        await CreateSut().OrchestrateAsync(CreateAccount(), CreateSyncConfig(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onJobCompleted: args =>
        {
            completedJobs.Add(args.Job);

            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);

        var reported = completedJobs.ShouldHaveSingleItem();
        reported.ShouldBeOfType<DeleteSyncJob>();
        reported.Target.RelativePath.ShouldBe("/Documents/Old");
        reported.Remote.RemoteItemId.Value.ShouldBe("folder-1");
    }

    [Fact]
    public async Task when_only_a_remote_deletion_is_applied_locally_then_no_changes_is_not_reported()
    {
        SetupRemoteDeletions(DeletedRemoteFolder());
        var progressMessages = new List<string>();

        await CreateSut().OrchestrateAsync(CreateAccount(), CreateSyncConfig(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), cancellationToken: TestContext.Current.CancellationToken);

        progressMessages.ShouldNotContain("Sync.NoChanges");
    }

    [Fact]
    public async Task when_nothing_is_deleted_remotely_then_no_job_is_reported_and_no_changes_is_raised()
    {
        SetupRemoteDeletions();
        var completedJobs = new List<SyncJob>();
        var progressMessages = new List<string>();

        await CreateSut().OrchestrateAsync(CreateAccount(), CreateSyncConfig(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), onJobCompleted: args =>
        {
            completedJobs.Add(args.Job);

            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);

        completedJobs.ShouldBeEmpty();
        progressMessages.ShouldContain("Sync.NoChanges");
    }
}
