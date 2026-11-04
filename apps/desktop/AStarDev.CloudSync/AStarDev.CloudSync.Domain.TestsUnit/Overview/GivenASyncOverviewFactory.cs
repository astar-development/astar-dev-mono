using AStarDev.CloudSync.Domain.Overview;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Domain.TestsUnit.Overview;

public class GivenASyncOverviewFactory
{
    [Fact]
    public void when_created_with_bytes_moved_today_then_the_overview_keeps_them()
    {
        var overview = SyncOverviewFactory.Create([], [], [], [], [], 3_328_599_655, Option.None<DateTimeOffset>());

        overview.BytesMovedToday.ShouldBe(3_328_599_655);
    }

    [Fact]
    public void when_created_with_negative_bytes_moved_today_then_they_are_normalised_to_zero()
    {
        var overview = SyncOverviewFactory.Create([], [], [], [], [], -1, Option.None<DateTimeOffset>());

        overview.BytesMovedToday.ShouldBe(0);
    }

    [Fact]
    public void when_created_with_a_last_checked_time_then_the_overview_keeps_it()
    {
        var overview = SyncOverviewFactory.Create([], [], [], [], [], 0, Option.Some(TestData.Now));

        overview.LastChecked.ShouldBe(Option.Some(TestData.Now));
    }
}
