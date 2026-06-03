namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAStorageQuota
{
    private const long TotalBytes = 1_099_511_627_776L;
    private const long UsedBytes = 549_755_813_888L;

    [Fact]
    public void when_created_then_total_bytes_is_set_correctly()
    {
        var quota = StorageQuotaFactory.Create(TotalBytes, UsedBytes);

        quota.TotalBytes.ShouldBe(TotalBytes);
    }

    [Fact]
    public void when_created_then_used_bytes_is_set_correctly()
    {
        var quota = StorageQuotaFactory.Create(TotalBytes, UsedBytes);

        quota.UsedBytes.ShouldBe(UsedBytes);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = StorageQuotaFactory.Create(TotalBytes, UsedBytes);
        var second = StorageQuotaFactory.Create(TotalBytes, UsedBytes);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_total_bytes_then_they_are_not_equal()
    {
        var first = StorageQuotaFactory.Create(TotalBytes, UsedBytes);
        var second = StorageQuotaFactory.Create(TotalBytes + 1, UsedBytes);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_used_bytes_then_they_are_not_equal()
    {
        var first = StorageQuotaFactory.Create(TotalBytes, UsedBytes);
        var second = StorageQuotaFactory.Create(TotalBytes, UsedBytes + 1);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_unknown_is_requested_then_both_fields_are_zero()
    {
        var unknown = StorageQuotaFactory.Unknown;

        unknown.TotalBytes.ShouldBe(0L);
        unknown.UsedBytes.ShouldBe(0L);
    }

    [Fact]
    public void when_fraction_is_calculated_with_nonzero_total_then_result_is_used_divided_by_total()
    {
        var quota = StorageQuotaFactory.Create(TotalBytes, UsedBytes);

        quota.Fraction().ShouldBe((double)UsedBytes / TotalBytes, tolerance: 1e-10);
    }

    [Fact]
    public void when_fraction_is_calculated_with_zero_total_then_result_is_zero()
    {
        var quota = StorageQuotaFactory.Unknown;

        quota.Fraction().ShouldBe(0.0);
    }

    [Fact]
    public void when_used_exceeds_total_then_fraction_is_clamped_to_one()
    {
        var quota = StorageQuotaFactory.Create(100L, 200L);

        quota.Fraction().ShouldBe(1.0);
    }
}
