using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncPassResult
{
    [Fact]
    public void when_created_with_did_run_true_then_did_run_is_true()
    {
        var sut = SyncPassResultFactory.Create(didRun: true, failedJobCount: 0);

        sut.DidRun.ShouldBeTrue();
    }

    [Fact]
    public void when_created_with_did_run_false_then_did_run_is_false()
    {
        var sut = SyncPassResultFactory.Create(didRun: false, failedJobCount: 0);

        sut.DidRun.ShouldBeFalse();
    }

    [Fact]
    public void when_created_with_failed_job_count_of_zero_then_failed_job_count_is_zero()
    {
        var sut = SyncPassResultFactory.Create(didRun: true, failedJobCount: 0);

        sut.FailedJobCount.ShouldBe(0);
    }

    [Fact]
    public void when_created_with_failed_job_count_of_three_then_failed_job_count_is_three()
    {
        var sut = SyncPassResultFactory.Create(didRun: false, failedJobCount: 3);

        sut.FailedJobCount.ShouldBe(3);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first  = SyncPassResultFactory.Create(didRun: true, failedJobCount: 2);
        var second = SyncPassResultFactory.Create(didRun: true, failedJobCount: 2);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_differ_in_did_run_then_they_are_not_equal()
    {
        var first  = SyncPassResultFactory.Create(didRun: true,  failedJobCount: 0);
        var second = SyncPassResultFactory.Create(didRun: false, failedJobCount: 0);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_differ_in_failed_job_count_then_they_are_not_equal()
    {
        var first  = SyncPassResultFactory.Create(didRun: true, failedJobCount: 1);
        var second = SyncPassResultFactory.Create(didRun: true, failedJobCount: 2);

        first.ShouldNotBe(second);
    }
}
