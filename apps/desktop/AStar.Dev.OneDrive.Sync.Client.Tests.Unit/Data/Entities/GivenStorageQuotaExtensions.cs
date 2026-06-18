namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenStorageQuotaExtensions
{
    [Fact]
    public void when_total_bytes_is_zero_then_fraction_is_zero()
    {
        var quota = StorageQuotaFactory.Create(0, 0);

        quota.Fraction().ShouldBe(0);
    }

    [Fact]
    public void when_used_equals_total_then_fraction_is_one()
    {
        var quota = StorageQuotaFactory.Create(1_000_000, 1_000_000);

        quota.Fraction().ShouldBe(1.0);
    }

    [Fact]
    public void when_used_is_half_of_total_then_fraction_is_point_five()
    {
        var quota = StorageQuotaFactory.Create(1_000_000, 500_000);

        quota.Fraction().ShouldBe(0.5);
    }

    [Fact]
    public void when_used_is_zero_then_fraction_is_zero()
    {
        var quota = StorageQuotaFactory.Create(1_000_000, 0);

        quota.Fraction().ShouldBe(0.0);
    }

    [Fact]
    public void when_used_exceeds_total_then_fraction_is_clamped_to_one()
    {
        var quota = StorageQuotaFactory.Create(1_000_000, 2_000_000);

        quota.Fraction().ShouldBe(1.0);
    }

    [Theory]
    [InlineData(100, 25, 0.25)]
    [InlineData(100, 75, 0.75)]
    [InlineData(200, 50, 0.25)]
    public void when_fraction_calculated_then_result_matches_expected(long total, long used, double expected)
    {
        var quota = StorageQuotaFactory.Create(total, used);

        quota.Fraction().ShouldBe(expected);
    }
}
