using AStarDev.OneDriveSyncClient.Dashboard.Throughput;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard.Throughput;

public sealed class GivenAThroughputBucketer
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Span = TimeSpan.FromMinutes(10);

    private static SyncThroughputSampleEntity Sample(TimeSpan age, long bytes) => new() { AccountId = new AccountId("acc-1"), CompletedAt = Now - age, Bytes = bytes };

    [Fact]
    public void when_there_are_no_samples_then_twelve_empty_buckets_are_returned()
    {
        var result = ThroughputBucketer.Bucket([], Now, 12, Span);

        result.Count.ShouldBe(12);
        result.ShouldAllBe(bytes => bytes == 0);
    }

    [Fact]
    public void when_a_sample_completed_just_now_then_it_is_in_the_newest_bucket()
    {
        var result = ThroughputBucketer.Bucket([Sample(TimeSpan.Zero, 500)], Now, 12, Span);

        result[11].ShouldBe(500);
    }

    [Fact]
    public void when_a_sample_completed_nearly_two_hours_ago_then_it_is_in_the_oldest_bucket()
    {
        var result = ThroughputBucketer.Bucket([Sample(TimeSpan.FromMinutes(119), 700)], Now, 12, Span);

        result[0].ShouldBe(700);
    }

    [Fact]
    public void when_a_sample_is_older_than_two_hours_then_it_is_excluded()
    {
        var result = ThroughputBucketer.Bucket([Sample(TimeSpan.FromMinutes(121), 900)], Now, 12, Span);

        result.Sum().ShouldBe(0);
    }

    [Fact]
    public void when_a_sample_is_in_the_future_then_it_is_excluded()
    {
        var result = ThroughputBucketer.Bucket([Sample(TimeSpan.FromMinutes(-5), 900)], Now, 12, Span);

        result.Sum().ShouldBe(0);
    }

    [Fact]
    public void when_samples_share_a_bucket_then_their_bytes_are_summed()
    {
        var result = ThroughputBucketer.Bucket([Sample(TimeSpan.FromMinutes(1), 100), Sample(TimeSpan.FromMinutes(2), 250)], Now, 12, Span);

        result[11].ShouldBe(350);
    }

    [Fact]
    public void when_a_sample_is_exactly_one_bucket_old_then_it_is_in_the_previous_bucket()
    {
        var result = ThroughputBucketer.Bucket([Sample(TimeSpan.FromMinutes(10), 40)], Now, 12, Span);

        result[10].ShouldBe(40);
    }
}
