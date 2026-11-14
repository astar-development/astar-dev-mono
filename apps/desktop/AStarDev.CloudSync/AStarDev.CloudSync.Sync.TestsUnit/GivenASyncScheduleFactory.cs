namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncScheduleFactory
{
    [Fact]
    public void when_the_default_schedule_is_created_then_it_is_every_hour()
    {
        SyncScheduleFactory.CreateDefault().ShouldBe(SyncScheduleFactory.CreateEvery(TimeSpan.FromHours(1)));
        SyncScheduleFactory.CreateDefault().ShouldBeOfType<SyncSchedule.Every>().Interval.ShouldBe(TimeSpan.FromHours(1));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(240)]
    public void when_an_interval_in_the_list_is_given_then_it_is_kept(int minutes)
    {
        SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(minutes)).ShouldBeOfType<SyncSchedule.Every>().Interval.ShouldBe(TimeSpan.FromMinutes(minutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(45)]
    [InlineData(1_000)]
    public void when_an_interval_not_in_the_list_is_given_then_it_becomes_one_hour(int minutes)
    {
        SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(minutes)).ShouldBeOfType<SyncSchedule.Every>().Interval.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void when_the_choices_are_listed_then_they_are_the_five_fixed_intervals()
    {
        SyncScheduleFactory.Intervals.ShouldBe([TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(4)]);
    }

    [Fact]
    public void when_the_manual_only_schedule_is_created_then_it_is_not_an_interval()
    {
        SyncScheduleFactory.CreateManualOnly().ShouldBeOfType<SyncSchedule.ManualOnly>();
    }

    [Fact]
    public void when_the_options_are_created_with_bad_values_then_they_are_normalised()
    {
        var options = SyncSchedulerOptionsFactory.Create(TimeSpan.FromMinutes(-1), TimeSpan.Zero);

        (options.StartupDelay, options.CheckEvery).ShouldBe((TimeSpan.Zero, TimeSpan.FromSeconds(30)));
    }
}
