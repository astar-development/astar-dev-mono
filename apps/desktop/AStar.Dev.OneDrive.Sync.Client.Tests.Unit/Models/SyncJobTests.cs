using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

public sealed class SyncJobTests
{
    private static SyncJob CreateMinimalJob() => SyncJobFactory.Create(accountId: "", folderId: "", remoteItemId: "", relativePath: "", localPath: "", direction: default, fileSize: 0, remoteModified: default);

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Id.ShouldNotBe(Guid.Empty);
        syncJob.AccountId.ShouldBe(string.Empty);
        syncJob.FolderId.ShouldBe(string.Empty);
        syncJob.RemoteItemId.ShouldBe(string.Empty);
        syncJob.RelativePath.ShouldBe(string.Empty);
        syncJob.LocalPath.ShouldBe(string.Empty);
        syncJob.Direction.ShouldBe(default);
        syncJob.State.ShouldBe(SyncJobState.Queued);
        syncJob.ErrorMessage.ShouldBeNull();
        syncJob.DownloadUrl.ShouldBeNull();
        syncJob.FileSize.ShouldBe(0L);
        syncJob.RemoteModified.ShouldBe(default);
        syncJob.QueuedAt.ShouldNotBe(default);
        syncJob.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        var job1 = CreateMinimalJob();
        var job2 = CreateMinimalJob();

        job1.Id.ShouldNotBe(job2.Id);
    }

    [Fact]
    public void CanCreateWithInitProperties()
    {
        var id = Guid.NewGuid();
        string accountId = "account-123";
        string folderId = "folder-456";
        string remoteItemId = "item-789";
        string relativePath = "Documents/report.pdf";
        string localPath = "/home/jason/Documents/report.pdf";
        var direction = SyncDirection.Download;
        long fileSize = 1024L;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);
        var queuedAt = DateTimeOffset.UtcNow;

        var syncJob = SyncJobFactory.Create(accountId, folderId, remoteItemId, relativePath, localPath, direction, fileSize, remoteModified) with { Id = id, QueuedAt = queuedAt };

        syncJob.Id.ShouldBe(id);
        syncJob.AccountId.ShouldBe(accountId);
        syncJob.FolderId.ShouldBe(folderId);
        syncJob.RemoteItemId.ShouldBe(remoteItemId);
        syncJob.RelativePath.ShouldBe(relativePath);
        syncJob.LocalPath.ShouldBe(localPath);
        syncJob.Direction.ShouldBe(direction);
        syncJob.FileSize.ShouldBe(fileSize);
        syncJob.RemoteModified.ShouldBe(remoteModified);
    }

    [Fact]
    public void when_copied_with_new_state_then_state_is_updated()
    {
        var syncJob = CreateMinimalJob() with { State = SyncJobState.InProgress };

        syncJob.State.ShouldBe(SyncJobState.InProgress);
    }

    [Theory]
    [InlineData(SyncJobState.Queued)]
    [InlineData(SyncJobState.InProgress)]
    [InlineData(SyncJobState.Completed)]
    [InlineData(SyncJobState.Failed)]
    [InlineData(SyncJobState.Skipped)]
    public void State_ShouldSupportAllStates(SyncJobState state)
    {
        var syncJob = CreateMinimalJob() with { State = state };

        syncJob.State.ShouldBe(state);
    }

    [Fact]
    public void when_copied_with_error_message_then_error_message_is_set()
    {
        string errorMessage = "File is locked by another process";

        var syncJob = CreateMinimalJob() with { ErrorMessage = errorMessage };

        syncJob.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void when_copied_with_download_url_then_download_url_is_set()
    {
        string downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc123/items/xyz789/content";

        var syncJob = CreateMinimalJob() with { DownloadUrl = downloadUrl };

        syncJob.DownloadUrl.ShouldBe(downloadUrl);
    }

    [Fact]
    public void when_copied_with_completed_at_then_completed_at_is_set()
    {
        var completedAt = DateTimeOffset.UtcNow;

        var syncJob = CreateMinimalJob() with { CompletedAt = completedAt };

        syncJob.CompletedAt.ShouldBe(completedAt);
    }

    [Theory]
    [InlineData(SyncDirection.Download)]
    [InlineData(SyncDirection.Upload)]
    [InlineData(SyncDirection.Delete)]
    public void Direction_ShouldSupportAllDirections(SyncDirection direction)
    {
        var syncJob = SyncJobFactory.Create(accountId: "", folderId: "", remoteItemId: "", relativePath: "", localPath: "", direction: direction, fileSize: 0, remoteModified: default);

        syncJob.Direction.ShouldBe(direction);
    }

    [Fact]
    public void QueuedAt_ShouldBeSetToCurrentUtcTimeByDefault()
    {
        var beforeCreation = DateTimeOffset.UtcNow;

        var syncJob = CreateMinimalJob();

        syncJob.QueuedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        syncJob.QueuedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void IsRecord_ShouldAllowValueEquality()
    {
        var id = Guid.NewGuid();
        var queuedAt = DateTimeOffset.UtcNow;
        var job1 = CreateMinimalJob() with { Id = id, AccountId = "account-123", QueuedAt = queuedAt };
        var job2 = CreateMinimalJob() with { Id = id, AccountId = "account-123", QueuedAt = queuedAt };

        job1.ShouldBe(job2);
    }

    [Fact]
    public void IsRecord_ShouldDifferOnPropertyChange()
    {
        var id = Guid.NewGuid();
        var job1 = CreateMinimalJob() with { Id = id, AccountId = "account-123" };
        var job2 = CreateMinimalJob() with { Id = id, AccountId = "account-456" };

        job1.ShouldNotBe(job2);
    }

    [Fact]
    public void DownloadJob_ShouldHaveCorrectProperties()
    {
        var downloadJob = SyncJobFactory.Create(accountId: "account-123", folderId: "folder-456", remoteItemId: "item-789", relativePath: "", localPath: "", direction: SyncDirection.Download, fileSize: 0, remoteModified: default);

        downloadJob.Direction.ShouldBe(SyncDirection.Download);
        downloadJob.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void UploadJob_ShouldHaveCorrectProperties()
    {
        var uploadJob = SyncJobFactory.Create(accountId: "account-123", folderId: "folder-456", remoteItemId: "item-789", relativePath: "", localPath: "", direction: SyncDirection.Upload, fileSize: 0, remoteModified: default);

        uploadJob.Direction.ShouldBe(SyncDirection.Upload);
        uploadJob.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void DeleteJob_ShouldHaveCorrectProperties()
    {
        var deleteJob = SyncJobFactory.Create(accountId: "account-123", folderId: "folder-456", remoteItemId: "item-789", relativePath: "", localPath: "", direction: SyncDirection.Delete, fileSize: 0, remoteModified: default);

        deleteJob.Direction.ShouldBe(SyncDirection.Delete);
        deleteJob.State.ShouldBe(SyncJobState.Queued);
    }
}
