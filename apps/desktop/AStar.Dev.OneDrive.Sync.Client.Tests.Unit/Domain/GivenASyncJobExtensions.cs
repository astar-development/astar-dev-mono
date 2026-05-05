using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncJobExtensions
{
    private static SyncJob CreateMinimalJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789"));
        var target = SyncFileTargetFactory.Create("/home/user/file.txt", "file.txt");
        var metadata = SyncFileMetadataFactory.Create(1024L, DateTimeOffset.UtcNow.AddHours(-1));
        var status = SyncJobStatusFactory.Create();

        return SyncJobFactory.Create(remote, target, metadata, SyncDirection.Download, status);
    }

    [Fact]
    public void when_complete_is_called_then_state_is_set_to_completed()
    {
        var job = CreateMinimalJob();

        var completed = job.Complete();

        completed.Status.State.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public void when_complete_is_called_then_completed_at_is_approximately_utc_now()
    {
        var job = CreateMinimalJob();
        var before = DateTimeOffset.UtcNow;

        var completed = job.Complete();

        completed.Status.CompletedAt.ShouldNotBeNull();
        completed.Status.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(before);
        completed.Status.CompletedAt.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void when_complete_is_called_then_all_other_fields_are_preserved()
    {
        var job = CreateMinimalJob();

        var completed = job.Complete();

        completed.Remote.ShouldBe(job.Remote);
        completed.Target.ShouldBe(job.Target);
        completed.Metadata.ShouldBe(job.Metadata);
        completed.Direction.ShouldBe(job.Direction);
        completed.Status.Id.ShouldBe(job.Status.Id);
        completed.Status.QueuedAt.ShouldBe(job.Status.QueuedAt);
        completed.DownloadUrl.ShouldBe(job.DownloadUrl);
        completed.UploadedRemoteItemId.ShouldBe(job.UploadedRemoteItemId);
    }

    [Fact]
    public void when_fail_is_called_with_null_error_message_then_state_is_set_to_failed()
    {
        var job = CreateMinimalJob();

        var failed = job.Fail(null);

        failed.Status.State.ShouldBe(SyncJobState.Failed);
    }

    [Fact]
    public void when_fail_is_called_with_null_error_message_then_error_message_is_null()
    {
        var job = CreateMinimalJob();

        var failed = job.Fail(null);

        failed.Status.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void when_fail_is_called_with_null_error_message_then_completed_at_is_approximately_utc_now()
    {
        var job = CreateMinimalJob();
        var before = DateTimeOffset.UtcNow;

        var failed = job.Fail(null);

        failed.Status.CompletedAt.ShouldNotBeNull();
        failed.Status.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(before);
        failed.Status.CompletedAt.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void when_fail_is_called_with_an_error_message_then_state_is_set_to_failed()
    {
        var job = CreateMinimalJob();

        var failed = job.Fail("Upload timed out");

        failed.Status.State.ShouldBe(SyncJobState.Failed);
    }

    [Fact]
    public void when_fail_is_called_with_an_error_message_then_error_message_is_set()
    {
        var job = CreateMinimalJob();
        const string errorMessage = "Upload timed out";

        var failed = job.Fail(errorMessage);

        failed.Status.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void when_fail_is_called_with_an_error_message_then_completed_at_is_approximately_utc_now()
    {
        var job = CreateMinimalJob();
        var before = DateTimeOffset.UtcNow;

        var failed = job.Fail("Upload timed out");

        failed.Status.CompletedAt.ShouldNotBeNull();
        failed.Status.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(before);
        failed.Status.CompletedAt.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void when_complete_is_called_then_original_job_is_unchanged()
    {
        var job = CreateMinimalJob();

        _ = job.Complete();

        job.Status.State.ShouldBe(SyncJobState.Queued);
        job.Status.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void when_complete_is_called_then_a_new_instance_is_returned()
    {
        var job = CreateMinimalJob();

        var completed = job.Complete();

        completed.ShouldNotBeSameAs(job);
    }
}
