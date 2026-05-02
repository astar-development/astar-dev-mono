using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenAParallelDownloadPipeline
{
    private const string AccountId  = "account-1";
    private const string FolderId   = "folder-1";
    private const string AccessToken = "test-token";

    private readonly IHttpDownloader  _downloader     = Substitute.For<IHttpDownloader>();
    private readonly IGraphService    _graphService   = Substitute.For<IGraphService>();
    private readonly ISyncRepository  _syncRepository = Substitute.For<ISyncRepository>();
    private readonly IFileSystem      _fileSystem     = Substitute.For<IFileSystem>();

    private ParallelDownloadPipeline CreateSut() => new(_syncRepository, _graphService, _downloader, _fileSystem);

    private static SyncJob MakeDownloadJob(string relativePath = "folder/file.txt")
        => SyncJobFactory.Create(accountId: "", folderId: "", remoteItemId: "", relativePath: relativePath, localPath: "/tmp/test-file.txt", direction: SyncDirection.Download, fileSize: 0, remoteModified: DateTimeOffset.UtcNow, downloadUrl: "https://example.com/file");

    [Fact]
    public async Task when_job_list_is_empty_then_on_progress_is_never_called()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([], AccessToken, progressEvents.Add, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        progressEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_job_list_is_empty_then_on_job_completed_is_never_called()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([], AccessToken, _ => { }, completedEvents.Add, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        completedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_job_list_is_empty_then_clear_completed_jobs_is_never_called()
    {
        var sut = CreateSut();

        await sut.RunAsync([], AccessToken, _ => { }, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<AccountId>());
    }

    [Fact]
    public async Task when_single_download_job_is_processed_then_on_job_completed_is_called_exactly_once()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, completedEvents.Add, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        completedEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_three_download_jobs_are_processed_then_on_job_completed_is_called_three_times()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var jobs = new[] { MakeDownloadJob("a/1.txt"), MakeDownloadJob("a/2.txt"), MakeDownloadJob("a/3.txt") };
        var sut = CreateSut();

        await sut.RunAsync(jobs, AccessToken, _ => { }, completedEvents.Add, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        completedEvents.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_jobs_complete_then_clear_completed_jobs_is_called_exactly_once()
    {
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ClearCompletedJobsAsync(Arg.Any<AccountId>());
    }

    [Fact]
    public async Task when_jobs_complete_then_clear_completed_jobs_is_called_with_correct_account_id()
    {
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ClearCompletedJobsAsync(new AccountId(AccountId));
    }

    [Fact]
    public async Task when_jobs_complete_then_final_progress_event_has_sync_state_idle()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, progressEvents.Add, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        progressEvents[^1].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_jobs_complete_then_final_progress_event_has_empty_current_file()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, progressEvents.Add, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        progressEvents[^1].CurrentFile.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_single_download_job_succeeds_then_on_job_completed_receives_completed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, completedEvents.Add, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

        completedEvents[0].Job.State.ShouldBe(SyncJobState.Completed);
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
        }, _ => { }, AccountId, FolderId, ct: TestContext.Current.CancellationToken);

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
        }, _ => { }, AccountId, FolderId, workerCount: 1, ct: TestContext.Current.CancellationToken);

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
            await sut.RunAsync([MakeDownloadJob()], AccessToken, _ => { }, _ => { }, AccountId, FolderId, ct: cts.Token);
        }
        catch(OperationCanceledException) { }

        await _syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<AccountId>());
    }
}
