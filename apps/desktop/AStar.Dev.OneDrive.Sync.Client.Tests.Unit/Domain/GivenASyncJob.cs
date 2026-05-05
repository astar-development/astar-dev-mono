using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncJob
{
    private static SyncJob CreateMinimalJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create();

        return SyncJobFactory.Create(remote, target, metadata, default, status);
    }

    [Fact]
    public void when_created_then_status_id_is_not_empty()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Status.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void when_created_then_remote_account_id_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Remote.AccountId.Id.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_created_then_remote_folder_id_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Remote.FolderId.Id.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_created_then_remote_item_id_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Remote.RemoteItemId.Id.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_created_then_target_relative_path_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Target.RelativePath.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_created_then_target_local_path_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Target.LocalPath.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_created_then_direction_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Direction.ShouldBe(default(SyncDirection));
    }

    [Fact]
    public void when_created_then_status_state_defaults_to_queued()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_created_then_status_error_message_is_null()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Status.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void when_created_then_download_url_is_null()
    {
        var syncJob = CreateMinimalJob();

        syncJob.DownloadUrl.ShouldBeNull();
    }

    [Fact]
    public void when_created_then_metadata_file_size_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Metadata.FileSize.ShouldBe(0L);
    }

    [Fact]
    public void when_created_then_metadata_remote_modified_matches_factory_input()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Metadata.RemoteModified.ShouldBe(default(DateTimeOffset));
    }

    [Fact]
    public void when_created_then_status_queued_at_is_not_default()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Status.QueuedAt.ShouldNotBe(default(DateTimeOffset));
    }

    [Fact]
    public void when_created_then_status_completed_at_is_null()
    {
        var syncJob = CreateMinimalJob();

        syncJob.Status.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void when_created_twice_then_status_ids_are_different()
    {
        var job1 = CreateMinimalJob();
        var job2 = CreateMinimalJob();

        job1.Status.Id.ShouldNotBe(job2.Status.Id);
    }

    [Fact]
    public void when_created_with_all_properties_then_values_are_correct()
    {
        var jobId = Guid.NewGuid();
        var queuedAt = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);
        const string accountId = "account-123";
        const string folderId = "folder-456";
        const string remoteItemId = "item-789";
        const string relativePath = "Documents/report.pdf";
        const string localPath = "/home/jason/Documents/report.pdf";
        const long fileSize = 1024L;

        var remote = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId(folderId), new OneDriveItemId(remoteItemId));
        var target = SyncFileTargetFactory.Create(localPath, relativePath);
        var metadata = SyncFileMetadataFactory.Create(fileSize, remoteModified);
        var status = SyncJobStatusFactory.Create();
        var syncJob = SyncJobFactory.Create(remote, target, metadata, SyncDirection.Download, status) with { Status = status with { Id = jobId, QueuedAt = queuedAt } };

        syncJob.Status.Id.ShouldBe(jobId);
        syncJob.Remote.AccountId.Id.ShouldBe(accountId);
        syncJob.Remote.FolderId.Id.ShouldBe(folderId);
        syncJob.Remote.RemoteItemId.Id.ShouldBe(remoteItemId);
        syncJob.Target.RelativePath.ShouldBe(relativePath);
        syncJob.Target.LocalPath.ShouldBe(localPath);
        syncJob.Direction.ShouldBe(SyncDirection.Download);
        syncJob.Metadata.FileSize.ShouldBe(fileSize);
        syncJob.Metadata.RemoteModified.ShouldBe(remoteModified);
    }

    [Fact]
    public void when_copied_with_new_state_then_state_is_updated()
    {
        var syncJob = CreateMinimalJob() with { Status = CreateMinimalJob().Status with { State = SyncJobState.InProgress } };

        syncJob.Status.State.ShouldBe(SyncJobState.InProgress);
    }

    [Theory]
    [InlineData(SyncJobState.Queued)]
    [InlineData(SyncJobState.InProgress)]
    [InlineData(SyncJobState.Completed)]
    [InlineData(SyncJobState.Failed)]
    [InlineData(SyncJobState.Skipped)]
    public void when_status_state_is_set_then_all_states_are_supported(SyncJobState state)
    {
        var syncJob = CreateMinimalJob() with { Status = CreateMinimalJob().Status with { State = state } };

        syncJob.Status.State.ShouldBe(state);
    }

    [Fact]
    public void when_copied_with_error_message_then_error_message_is_set()
    {
        const string errorMessage = "File is locked by another process";
        var original = CreateMinimalJob();

        var syncJob = original with { Status = original.Status with { ErrorMessage = errorMessage } };

        syncJob.Status.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void when_copied_with_download_url_then_download_url_is_set()
    {
        const string downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc123/items/xyz789/content";

        var syncJob = CreateMinimalJob() with { DownloadUrl = downloadUrl };

        syncJob.DownloadUrl.ShouldBe(downloadUrl);
    }

    [Fact]
    public void when_copied_with_completed_at_then_completed_at_is_set()
    {
        var completedAt = DateTimeOffset.UtcNow;
        var original = CreateMinimalJob();

        var syncJob = original with { Status = original.Status with { CompletedAt = completedAt } };

        syncJob.Status.CompletedAt.ShouldBe(completedAt);
    }

    [Theory]
    [InlineData(SyncDirection.Download)]
    [InlineData(SyncDirection.Upload)]
    [InlineData(SyncDirection.Delete)]
    public void when_direction_is_set_then_all_directions_are_supported(SyncDirection direction)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create();

        var syncJob = SyncJobFactory.Create(remote, target, metadata, direction, status);

        syncJob.Direction.ShouldBe(direction);
    }

    [Fact]
    public void when_created_then_status_queued_at_is_set_to_approximately_utc_now()
    {
        var beforeCreation = DateTimeOffset.UtcNow;

        var syncJob = CreateMinimalJob();

        syncJob.Status.QueuedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        syncJob.Status.QueuedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void when_two_jobs_have_same_values_then_they_are_equal()
    {
        var jobId = Guid.NewGuid();
        var queuedAt = DateTimeOffset.UtcNow;
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create() with { Id = jobId, QueuedAt = queuedAt };
        var job1 = SyncJobFactory.Create(remote, target, metadata, default, status);
        var job2 = SyncJobFactory.Create(remote, target, metadata, default, status);

        job1.ShouldBe(job2);
    }

    [Fact]
    public void when_two_jobs_differ_on_account_id_then_they_are_not_equal()
    {
        var jobId = Guid.NewGuid();
        var queuedAt = DateTimeOffset.UtcNow;
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create() with { Id = jobId, QueuedAt = queuedAt };
        var job1 = SyncJobFactory.Create(RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId(""), new OneDriveItemId("")), target, metadata, default, status);
        var job2 = SyncJobFactory.Create(RemoteItemRefFactory.Create(new AccountId("account-456"), new OneDriveFolderId(""), new OneDriveItemId("")), target, metadata, default, status);

        job1.ShouldNotBe(job2);
    }

    [Fact]
    public void when_direction_is_download_then_state_defaults_to_queued()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789"));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create();

        var downloadJob = SyncJobFactory.Create(remote, target, metadata, SyncDirection.Download, status);

        downloadJob.Direction.ShouldBe(SyncDirection.Download);
        downloadJob.Status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_direction_is_upload_then_state_defaults_to_queued()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789"));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create();

        var uploadJob = SyncJobFactory.Create(remote, target, metadata, SyncDirection.Upload, status);

        uploadJob.Direction.ShouldBe(SyncDirection.Upload);
        uploadJob.Status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_direction_is_delete_then_state_defaults_to_queued()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789"));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var status = SyncJobStatusFactory.Create();

        var deleteJob = SyncJobFactory.Create(remote, target, metadata, SyncDirection.Delete, status);

        deleteJob.Direction.ShouldBe(SyncDirection.Delete);
        deleteJob.Status.State.ShouldBe(SyncJobState.Queued);
    }
}
