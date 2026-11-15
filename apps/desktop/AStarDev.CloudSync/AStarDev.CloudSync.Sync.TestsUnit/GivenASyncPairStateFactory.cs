using AStarDev.FunctionalParadigm;
using Shouldly;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncPairStateFactory
{
    [Fact]
    public void when_the_file_count_is_negative_then_it_is_normalised_to_zero()
    {
        var state = SyncPairStateFactory.Create(SyncStatusFactory.CreateUpToDate(), -5, Option.None<DateTimeOffset>());

        state.FileCount.ShouldBe(0);
    }

    [Fact]
    public void when_the_file_count_is_positive_then_it_is_kept()
    {
        var state = SyncPairStateFactory.Create(SyncStatusFactory.CreateUpToDate(), 12, Option.None<DateTimeOffset>());

        state.FileCount.ShouldBe(12);
    }
}
