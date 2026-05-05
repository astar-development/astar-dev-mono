using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncJobStatus
{
    [Fact]
    public void when_created_then_id_is_not_empty()
    {
        var status = SyncJobStatusFactory.Create();

        status.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void when_created_then_queued_at_is_approximately_utc_now()
    {
        var before = DateTimeOffset.UtcNow;

        var status = SyncJobStatusFactory.Create();

        status.QueuedAt.ShouldBeGreaterThanOrEqualTo(before);
        status.QueuedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void when_created_then_state_defaults_to_queued()
    {
        var status = SyncJobStatusFactory.Create();

        status.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void when_created_then_error_message_is_null()
    {
        var status = SyncJobStatusFactory.Create();

        status.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void when_created_then_completed_at_is_null()
    {
        var status = SyncJobStatusFactory.Create();

        status.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void when_created_twice_then_ids_are_different()
    {
        var firstStatus = SyncJobStatusFactory.Create();
        var secondStatus = SyncJobStatusFactory.Create();

        firstStatus.Id.ShouldNotBe(secondStatus.Id);
    }
}
