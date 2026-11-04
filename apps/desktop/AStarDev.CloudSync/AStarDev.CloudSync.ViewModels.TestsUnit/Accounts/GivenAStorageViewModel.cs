using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Accounts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAStorageViewModel
{
    [Fact]
    public void when_storage_is_partly_used_then_the_text_shows_used_of_total()
    {
        var storage = new StorageViewModel(StorageUsageFactory.Create(782_643_000_000, 1_099_511_627_776), TestData.CreateLocalization());

        storage.Text.ShouldBe("728.9 GB of 1.0 TB");
    }

    [Fact]
    public void when_storage_is_partly_used_then_the_fraction_is_used_over_total()
    {
        var storage = new StorageViewModel(StorageUsageFactory.Create(250, 1000), TestData.CreateLocalization());

        storage.Fraction.ShouldBe(0.25);
    }

    [Fact]
    public void when_the_total_is_zero_then_the_fraction_is_zero()
    {
        var storage = new StorageViewModel(StorageUsageFactory.Create(0, 0), TestData.CreateLocalization());

        storage.Fraction.ShouldBe(0);
    }

    [Fact]
    public void when_negative_sizes_are_supplied_then_they_are_normalised_to_zero()
    {
        var usage = StorageUsageFactory.Create(-5, -10);

        usage.UsedBytes.ShouldBe(0);
        usage.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void when_used_exceeds_total_then_the_fraction_is_capped_at_one()
    {
        var storage = new StorageViewModel(StorageUsageFactory.Create(2000, 1000), TestData.CreateLocalization());

        storage.Fraction.ShouldBe(1);
    }
}
