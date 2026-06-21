using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassResultFactory
{
    [Fact]
    public void when_create_is_called_with_did_run_true_then_result_has_did_run_true()
    {
        var result = SyncPassResultFactory.Create(didRun: true, failedJobCount: 0);

        result.DidRun.ShouldBeTrue();
    }

    [Fact]
    public void when_create_is_called_with_did_run_false_then_result_has_did_run_false()
    {
        var result = SyncPassResultFactory.Create(didRun: false, failedJobCount: 0);

        result.DidRun.ShouldBeFalse();
    }

    [Fact]
    public void when_create_is_called_with_failed_job_count_of_zero_then_result_has_failed_job_count_zero()
    {
        var result = SyncPassResultFactory.Create(didRun: true, failedJobCount: 0);

        result.FailedJobCount.ShouldBe(0);
    }

    [Fact]
    public void when_create_is_called_with_failed_job_count_of_three_then_result_has_failed_job_count_three()
    {
        var result = SyncPassResultFactory.Create(didRun: false, failedJobCount: 3);

        result.FailedJobCount.ShouldBe(3);
    }

    [Fact]
    public void when_create_is_called_then_result_is_sync_pass_result()
    {
        var result = SyncPassResultFactory.Create(didRun: true, failedJobCount: 0);

        result.ShouldBeOfType<SyncPassResult>();
    }
}
