using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenAParallelDownloadPipeline
{
    private const string AccountIdValue = "account-1";
    private const string FolderIdValue  = "folder-1";
    private const string AccessToken = "test-token";

    private readonly IHttpDownloader  _downloader     = Substitute.For<IHttpDownloader>();
    private readonly IGraphService    _graphService   = Substitute.For<IGraphService>();
    private readonly ISyncRepository  _syncRepository = Substitute.For<ISyncRepository>();
    private readonly IFileSystem      _fileSystem     = Substitute.For<IFileSystem>();

    public GivenAParallelDownloadPipeline()
    {
        _downloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<global::System.Reactive.Unit, string>.Ok(global::System.Reactive.Unit.Default));
    }

    private ParallelDownloadPipeline CreateSut() => new(_syncRepository, _graphService, _downloader, _fileSystem);

    private static DownloadSyncJob MakeDownloadJob(string relativePath = "folder/file.txt")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("/tmp/test-file.txt", relativePath);
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    [Fact]
    public async Task when_job_list_is_empty_then_on_progress_is_never_called()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([], AccessToken, progressEvents.Add, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        progressEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_job_list_is_empty_then_on_job_completed_is_never_called()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([], AccessToken, _ => { }, completedEvents.Add, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_job_list_is_empty_then_clear_completed_jobs_is_never_called()
    {
        var sut = CreateSut();

        await sut.RunAsync([], AccessToken, _ => { }, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<AccountId>());
    }

    [Fact]
    public async Task when_single_download_job_is_processed_then_on_job_completed_is_called_exactly_once()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, completedEvents.Add, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_three_download_jobs_are_processed_then_on_job_completed_is_called_three_times()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var jobs = new[] { MakeDownloadJob("a/1.txt"), MakeDownloadJob("a/2.txt"), MakeDownloadJob("a/3.txt") };
        var sut = CreateSut();

        await sut.RunAsync(jobs, AccessToken, _ => { }, completedEvents.Add, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_jobs_complete_then_clear_completed_jobs_is_called_exactly_once()
    {
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ClearCompletedJobsAsync(Arg.Any<AccountId>());
    }

    [Fact]
    public async Task when_jobs_complete_then_clear_completed_jobs_is_called_with_correct_account_id()
    {
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ClearCompletedJobsAsync(new AccountId(AccountIdValue));
    }

    [Fact]
    public async Task when_jobs_complete_then_final_progress_event_has_sync_state_idle()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, progressEvents.Add, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        progressEvents[^1].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_jobs_complete_then_final_progress_event_has_empty_current_file()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, progressEvents.Add, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        progressEvents[^1].CurrentFile.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_single_download_job_succeeds_then_on_job_completed_receives_completed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, completedEvents.Add, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents[0].Job.Status.State.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public async Task when_single_job_finishes_then_per_job_progress_event_has_sync_state_idle()
    {
        var perJobProgressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, args =>
        {
            if(args.CurrentFile != string.Empty)
                perJobProgressEvents.Add(args);
        }, _ => { }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        perJobProgressEvents.Count.ShouldBe(1);
        perJobProgressEvents[0].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_two_jobs_run_then_first_per_job_progress_event_has_sync_state_syncing()
    {
        var perJobProgressEvents = new List<SyncProgressEventArgs>();
        var jobs = new[] { MakeDownloadJob("a/1.txt"), MakeDownloadJob("a/2.txt") };
        var sut = CreateSut();

        await sut.RunAsync(jobs, AccessToken, args =>
        {
            if(args.CurrentFile != string.Empty)
                perJobProgressEvents.Add(args);
        }, _ => { }, AccountIdValue, FolderIdValue, workerCount: 1, ct: TestContext.Current.CancellationToken);

        perJobProgressEvents.Count.ShouldBe(2);
        perJobProgressEvents[0].SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_token_is_cancelled_before_run_then_clear_completed_jobs_is_never_called()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = CreateSut();

        try
        {
            await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, _ => { }, AccountIdValue, FolderIdValue, ct: cts.Token);
        }
        catch(OperationCanceledException) { }

        await _syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<AccountId>());
    }
}
