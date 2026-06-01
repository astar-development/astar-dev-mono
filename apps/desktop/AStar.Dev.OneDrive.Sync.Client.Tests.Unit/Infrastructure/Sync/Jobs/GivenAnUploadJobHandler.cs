using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnUploadJobHandler
{
    private const string AccountId = "account-001";
    private const string AccessToken = "test-token";
    private const string ItemId = "item-abc";

    private readonly IGraphService _graphService = Substitute.For<IGraphService>();

    private UploadJobHandler CreateSut() => new(_graphService, NullLogger<UploadJobHandler>.Instance);

    private static UploadSyncJob MakeUploadJob(string folderId = "folder-1")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(folderId), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateUpload(remote, target, metadata);
    }

    private static DownloadSyncJob MakeDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private static DeleteSyncJob MakeDeleteJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDelete(remote, target, metadata);
    }

    [Fact]
    public void when_given_upload_sync_job_then_can_handle_returns_true()
    {
        var job = MakeUploadJob();

        CreateSut().CanHandle(job).ShouldBeTrue();
    }

    [Fact]
    public void when_given_download_sync_job_then_can_handle_returns_false()
    {
        var job = MakeDownloadJob();

        CreateSut().CanHandle(job).ShouldBeFalse();
    }

    [Fact]
    public void when_given_delete_sync_job_then_can_handle_returns_false()
    {
        var job = MakeDeleteJob();

        CreateSut().CanHandle(job).ShouldBeFalse();
    }

    [Fact]
    public async Task when_upload_succeeds_then_result_is_ok_with_job()
    {
        var job = MakeUploadJob();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult(AccessToken);

        _graphService.UploadFileAsync(AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), job.Target.LocalPath, Arg.Any<string>(), job.Remote.FolderId.Id, Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok("remote-item-id"));

        var result = await CreateSut().HandleAsync(job, AccountId, tokenFactory, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_upload_succeeds_then_graph_upload_is_called()
    {
        var job = MakeUploadJob();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult(AccessToken);

        _graphService.UploadFileAsync(AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), job.Target.LocalPath, Arg.Any<string>(), job.Remote.FolderId.Id, Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok("remote-item-id"));

        await CreateSut().HandleAsync(job, AccountId, tokenFactory, TestContext.Current.CancellationToken);

        await _graphService.Received(1).UploadFileAsync(AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), job.Target.LocalPath, Arg.Any<string>(), job.Remote.FolderId.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_upload_fails_then_result_is_error_with_message()
    {
        const string uploadError = "Upload failed: quota exceeded";
        var job = MakeUploadJob();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult(AccessToken);

        _graphService.UploadFileAsync(AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), job.Target.LocalPath, Arg.Any<string>(), job.Remote.FolderId.Id, Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Error(uploadError));

        var result = await CreateSut().HandleAsync(job, AccountId, tokenFactory, TestContext.Current.CancellationToken);

        result.Match<string?>(_ => null, error => error).ShouldBe(uploadError);
    }
}
