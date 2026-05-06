using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenATypedSyncJob
{
    private static RemoteItemRef MakeRemote() => RemoteItemRefFactory.Create(new AccountId("account-1"), new OneDriveFolderId("folder-1"), new OneDriveItemId("item-1"));

    private static SyncFileTarget MakeTarget() => SyncFileTargetFactory.Create("/local/path/file.txt", "Documents/file.txt");

    private static SyncFileMetadata MakeMetadata() => SyncFileMetadataFactory.Create(1024L, DateTimeOffset.UtcNow.AddHours(-1));

    [Fact]
    public void when_create_download_is_called_then_result_is_download_sync_job()
    {
        var result = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.ShouldBeOfType<DownloadSyncJob>();
    }

    [Fact]
    public void when_create_download_is_called_with_url_then_download_url_is_set()
    {
        const string url = "https://graph.microsoft.com/file";

        var result = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata(), url);

        result.DownloadUrl.ShouldBe(url);
    }

    [Fact]
    public void when_create_download_is_called_without_url_then_download_url_is_null()
    {
        var result = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.DownloadUrl.ShouldBeNull();
    }

    [Fact]
    public void when_create_download_is_called_then_status_state_defaults_to_queued()
    {
        var result = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.Status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_create_download_is_called_then_status_id_is_not_empty()
    {
        var result = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.Status.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void when_create_upload_is_called_then_result_is_upload_sync_job()
    {
        var result = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.ShouldBeOfType<UploadSyncJob>();
    }

    [Fact]
    public void when_create_upload_is_called_then_uploaded_remote_item_id_defaults_to_null()
    {
        var result = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.UploadedRemoteItemId.ShouldBeNull();
    }

    [Fact]
    public void when_create_upload_is_called_then_status_state_defaults_to_queued()
    {
        var result = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.Status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_create_upload_is_called_then_status_id_is_not_empty()
    {
        var result = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        result.Status.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void when_create_delete_is_called_then_result_is_delete_sync_job()
    {
        var result = SyncJobFactory.CreateDelete(MakeRemote(), MakeTarget(), MakeMetadata());

        result.ShouldBeOfType<DeleteSyncJob>();
    }

    [Fact]
    public void when_create_delete_is_called_then_status_state_defaults_to_queued()
    {
        var result = SyncJobFactory.CreateDelete(MakeRemote(), MakeTarget(), MakeMetadata());

        result.Status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_create_delete_is_called_then_status_id_is_not_empty()
    {
        var result = SyncJobFactory.CreateDelete(MakeRemote(), MakeTarget(), MakeMetadata());

        result.Status.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void when_download_sync_job_is_pattern_matched_then_download_url_is_accessible()
    {
        const string url = "https://graph.microsoft.com/file";
        SyncJob job = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata(), url);

        if(job is DownloadSyncJob downloadJob)
            downloadJob.DownloadUrl.ShouldBe(url);
        else
            Assert.Fail("Expected DownloadSyncJob");
    }

    [Fact]
    public void when_upload_sync_job_is_pattern_matched_then_uploaded_remote_item_id_is_accessible()
    {
        SyncJob job = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        if(job is UploadSyncJob uploadJob)
            uploadJob.UploadedRemoteItemId.ShouldBeNull();
        else
            Assert.Fail("Expected UploadSyncJob");
    }

    [Fact]
    public void when_delete_sync_job_is_pattern_matched_then_it_matches_correctly()
    {
        SyncJob job = SyncJobFactory.CreateDelete(MakeRemote(), MakeTarget(), MakeMetadata());

        (job is DeleteSyncJob).ShouldBeTrue();
    }

    [Fact]
    public void when_download_job_is_checked_then_it_is_not_upload_sync_job()
    {
        SyncJob job = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        (job is UploadSyncJob).ShouldBeFalse();
    }

    [Fact]
    public void when_download_job_is_checked_then_it_is_not_delete_sync_job()
    {
        SyncJob job = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        (job is DeleteSyncJob).ShouldBeFalse();
    }

    [Fact]
    public void when_upload_job_is_checked_then_it_is_not_download_sync_job()
    {
        SyncJob job = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        (job is DownloadSyncJob).ShouldBeFalse();
    }

    [Fact]
    public void when_all_jobs_are_base_type_then_remote_target_and_metadata_are_accessible()
    {
        var remote = MakeRemote();
        var target = MakeTarget();
        var metadata = MakeMetadata();
        SyncJob job = SyncJobFactory.CreateDownload(remote, target, metadata);

        job.Remote.ShouldBe(remote);
        job.Target.ShouldBe(target);
        job.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void when_two_download_jobs_are_created_then_they_have_different_status_ids()
    {
        var job1 = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());
        var job2 = SyncJobFactory.CreateDownload(MakeRemote(), MakeTarget(), MakeMetadata());

        job1.Status.Id.ShouldNotBe(job2.Status.Id);
    }

    [Fact]
    public void when_download_job_is_created_then_remote_properties_are_mapped_correctly()
    {
        var remote = MakeRemote();

        var result = SyncJobFactory.CreateDownload(remote, MakeTarget(), MakeMetadata());

        result.Remote.AccountId.Id.ShouldBe("account-1");
        result.Remote.FolderId.Id.ShouldBe("folder-1");
        result.Remote.RemoteItemId.Id.ShouldBe("item-1");
    }

    [Fact]
    public void when_upload_job_with_remote_item_id_is_copied_then_uploaded_id_is_updated()
    {
        var job = SyncJobFactory.CreateUpload(MakeRemote(), MakeTarget(), MakeMetadata());

        var updated = job with { UploadedRemoteItemId = "remote-456" };

        updated.UploadedRemoteItemId.ShouldBe("remote-456");
    }
}
