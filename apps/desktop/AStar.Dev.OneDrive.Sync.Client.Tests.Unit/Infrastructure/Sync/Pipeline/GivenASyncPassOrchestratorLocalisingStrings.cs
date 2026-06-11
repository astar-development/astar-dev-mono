using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Options;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassOrchestratorLocalisingStrings
{
    private readonly IAccountRepository      _accountRepository      = Substitute.For<IAccountRepository>();
    private readonly IDriveStateRepository   _driveStateRepository   = Substitute.For<IDriveStateRepository>();
    private readonly IRemoteFolderEnumerator _remoteFolderEnumerator = Substitute.For<IRemoteFolderEnumerator>();
    private readonly IRemoteDeletionDetector _remoteDeletionDetector = Substitute.For<IRemoteDeletionDetector>();
    private readonly ILocalDeletionDetector  _localDeletionDetector  = Substitute.For<ILocalDeletionDetector>();
    private readonly ILocalChangeDetector    _localChangeDetector    = Substitute.For<ILocalChangeDetector>();
    private readonly ISyncJobExecutor        _syncJobExecutor        = Substitute.For<ISyncJobExecutor>();
    private readonly IDownloadJobBuilder     _downloadJobBuilder     = Substitute.For<IDownloadJobBuilder>();
    private readonly ILocalizationService    _localizationService    = Substitute.For<ILocalizationService>();

    public GivenASyncPassOrchestratorLocalisingStrings()
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

    private static SyncJob CreateMinimalDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId("folder-1"), new OneDriveItemId("item-1"));
        var target = SyncFileTargetFactory.Create("/sync/file.txt", "file.txt");
        var metadata = SyncFileMetadataFactory.Create(1024L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata);
    }

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

    private void SetupWithOneDownloadJob()
    {
        _driveStateRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<DriveStateEntity>());
        _remoteFolderEnumerator.EnumerateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEnumerationResult());
        _downloadJobBuilder.BuildAsync(Arg.Any<OneDriveAccount>(), Arg.Any<IReadOnlyList<DeltaItem>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<Dictionary<string, SyncedItemEntity>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SyncJob>)[CreateMinimalDownloadJob()]);
        _localChangeDetector.DetectNewAndModifiedFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>())
            .Returns([]);
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<AccountEntity>());
    }

    [Fact]
    public async Task when_detecting_remote_deletions_then_localisation_key_Sync_DetectingRemoteDeletions_is_used()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.DetectingRemoteDeletions");
    }

    [Fact]
    public async Task when_detecting_local_changes_then_localisation_key_Sync_DetectingLocalChanges_is_used()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.DetectingLocalChanges");
    }

    [Fact]
    public async Task when_no_jobs_exist_then_localisation_key_Sync_NoChanges_is_used()
    {
        SetupDeepSyncPrerequisites();

        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: _ => { }, ct: TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.NoChanges");
    }

    [Fact]
    public async Task when_jobs_exist_then_localisation_key_Sync_SyncingFiles_is_used()
    {
        SetupWithOneDownloadJob();

        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, ct: TestContext.Current.CancellationToken);

        _localizationService.Received().GetLocal("Sync.SyncingFiles", Arg.Any<object[]>());
    }

    [Fact]
    public async Task when_detecting_remote_deletions_then_progress_message_is_localisation_key()
    {
        SetupDeepSyncPrerequisites();

        var progressMessages = new List<string>();
        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), ct: TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.DetectingRemoteDeletions");
    }

    [Fact]
    public async Task when_detecting_local_changes_then_progress_message_is_localisation_key()
    {
        SetupDeepSyncPrerequisites();

        var progressMessages = new List<string>();
        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), ct: TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.DetectingLocalChanges");
    }

    [Fact]
    public async Task when_no_jobs_exist_then_progress_message_is_no_changes_localisation_key()
    {
        SetupDeepSyncPrerequisites();

        var progressMessages = new List<string>();
        var sut = CreateSut();

        await sut.OrchestrateAsync(CreateAccount(), _ => Task.FromResult("token"), _ => Task.CompletedTask, onProgress: args => progressMessages.Add(args.CurrentFile), ct: TestContext.Current.CancellationToken);

        progressMessages.ShouldContain("Sync.NoChanges");
    }
}
