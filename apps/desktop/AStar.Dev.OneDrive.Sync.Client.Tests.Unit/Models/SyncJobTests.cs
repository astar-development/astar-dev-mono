using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

public class SyncJobTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        var syncJob = new SyncJob();

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
        var job1 = new SyncJob();
        var job2 = new SyncJob();

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

        var syncJob = new SyncJob
        {
            Id = id,
            AccountId = accountId,
            FolderId = folderId,
            RemoteItemId = remoteItemId,
            RelativePath = relativePath,
            LocalPath = localPath,
            Direction = direction,
            FileSize = fileSize,
            RemoteModified = remoteModified
        };

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
    public void State_ShouldBeSettable()
    {
        var syncJob = new SyncJob
        {
            State = SyncJobState.InProgress
        };

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
        var syncJob = new SyncJob
        {
            State = state
        };

        syncJob.State.ShouldBe(state);
    }

    [Fact]
    public void ErrorMessage_ShouldBeSettable()
    {
        var syncJob = new SyncJob();
        string errorMessage = "File is locked by another process";

        syncJob.ErrorMessage = errorMessage;

        syncJob.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void DownloadUrl_ShouldBeSettable()
    {
        var syncJob = new SyncJob();
        string downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc123/items/xyz789/content";

        syncJob.DownloadUrl = downloadUrl;

        syncJob.DownloadUrl.ShouldBe(downloadUrl);
    }

    [Fact]
    public void CompletedAt_ShouldBeSettable()
    {
        var syncJob = new SyncJob();
        var completedAt = DateTimeOffset.UtcNow;

        syncJob.CompletedAt = completedAt;

        syncJob.CompletedAt.ShouldBe(completedAt);
    }

    [Theory]
    [InlineData(SyncDirection.Download)]
    [InlineData(SyncDirection.Upload)]
    [InlineData(SyncDirection.Delete)]
    public void Direction_ShouldSupportAllDirections(SyncDirection direction)
    {
        var syncJob = new SyncJob { Direction = direction };

        syncJob.Direction.ShouldBe(direction);
    }

    [Fact]
    public void QueuedAt_ShouldBeSetToCurrentUtcTimeByDefault()
    {
        var beforeCreation = DateTimeOffset.UtcNow;

        var syncJob = new SyncJob();

        syncJob.QueuedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        syncJob.QueuedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void IsRecord_ShouldAllowValueEquality()
    {
        var id = Guid.NewGuid();
        var queuedAt = DateTimeOffset.UtcNow;
        var job1 = new SyncJob { Id = id, AccountId = "account-123", QueuedAt = queuedAt };
        var job2 = new SyncJob { Id = id, AccountId = "account-123", QueuedAt = queuedAt };

        job1.ShouldBe(job2);
    }

    [Fact]
    public void IsRecord_ShouldDifferOnPropertyChange()
    {
        var id = Guid.NewGuid();
        var job1 = new SyncJob { Id = id, AccountId = "account-123" };
        var job2 = new SyncJob { Id = id, AccountId = "account-456" };

        job1.ShouldNotBe(job2);
    }

    [Fact]
    public void DownloadJob_ShouldHaveCorrectProperties()
    {
        var downloadJob = new SyncJob
        {
            AccountId = "account-123",
            FolderId = "folder-456",
            RemoteItemId = "item-789",
            Direction = SyncDirection.Download,
            State = SyncJobState.Queued
        };

        downloadJob.Direction.ShouldBe(SyncDirection.Download);
        downloadJob.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void UploadJob_ShouldHaveCorrectProperties()
    {
        var uploadJob = new SyncJob
        {
            AccountId = "account-123",
            FolderId = "folder-456",
            RemoteItemId = "item-789",
            Direction = SyncDirection.Upload,
            State = SyncJobState.Queued
        };

        uploadJob.Direction.ShouldBe(SyncDirection.Upload);
        uploadJob.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void DeleteJob_ShouldHaveCorrectProperties()
    {
        var deleteJob = new SyncJob
        {
            AccountId = "account-123",
            FolderId = "folder-456",
            RemoteItemId = "item-789",
            Direction = SyncDirection.Delete,
            State = SyncJobState.Queued
        };

        deleteJob.Direction.ShouldBe(SyncDirection.Delete);
        deleteJob.State.ShouldBe(SyncJobState.Queued);
    }
}
