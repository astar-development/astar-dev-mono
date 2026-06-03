using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenADeleteJobHandler
{
    private const string ItemId = "item-abc";

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    private DeleteJobHandler CreateSut() => new(_fileSystem);

    private static DeleteSyncJob MakeDeleteJob(string localPath = "/tmp/file.txt")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create(localPath, "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDelete(remote, target, metadata);
    }

    private static DownloadSyncJob MakeDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private static UploadSyncJob MakeUploadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId("folder-1"), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateUpload(remote, target, metadata);
    }

    [Fact]
    public void when_given_delete_sync_job_then_can_handle_returns_true()
    {
        var job = MakeDeleteJob();

        CreateSut().CanHandle(job).ShouldBeTrue();
    }

    [Fact]
    public void when_given_download_sync_job_then_can_handle_returns_false()
    {
        var job = MakeDownloadJob();

        CreateSut().CanHandle(job).ShouldBeFalse();
    }

    [Fact]
    public void when_given_upload_sync_job_then_can_handle_returns_false()
    {
        var job = MakeUploadJob();

        CreateSut().CanHandle(job).ShouldBeFalse();
    }

    [Fact]
    public async Task when_file_exists_then_file_is_deleted()
    {
        const string localPath = "/tmp/existing-file.txt";
        var job = MakeDeleteJob(localPath);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("any-token");

        _fileSystem.File.Exists(localPath).Returns(true);

        await CreateSut().HandleAsync(job, string.Empty, tokenFactory, TestContext.Current.CancellationToken);

        _fileSystem.File.Received(1).Delete(localPath);
    }

    [Fact]
    public async Task when_file_does_not_exist_then_no_delete_is_called()
    {
        const string localPath = "/tmp/nonexistent-file.txt";
        var job = MakeDeleteJob(localPath);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("any-token");

        _fileSystem.File.Exists(localPath).Returns(false);

        await CreateSut().HandleAsync(job, string.Empty, tokenFactory, TestContext.Current.CancellationToken);

        _fileSystem.File.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public async Task when_delete_job_is_processed_then_result_is_ok()
    {
        var job = MakeDeleteJob();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("any-token");

        var result = await CreateSut().HandleAsync(job, string.Empty, tokenFactory, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }
}
