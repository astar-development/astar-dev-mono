namespace AStarDev.Utilities.Tests.Unit;

public class GivenTimeSpanExtensions
{
    [Fact]
    public void when_the_elapsed_is_less_than_two_minutes_then_is_just_now_returns_true()
        => TimeSpan.FromMinutes(1).IsJustNow().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_two_minutes_then_is_just_now_returns_false()
        => TimeSpan.FromMinutes(3).IsJustNow().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_less_than_one_hour_then_is_minutes_ago_returns_true()
        => TimeSpan.FromMinutes(30).IsMinutesAgo().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_one_hour_then_is_minutes_ago_returns_false()
        => TimeSpan.FromHours(2).IsMinutesAgo().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_less_than_one_day_then_is_hours_ago_returns_true()
        => TimeSpan.FromHours(12).IsHoursAgo().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_one_day_then_is_hours_ago_returns_false()
        => TimeSpan.FromDays(2).IsHoursAgo().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_less_than_two_days_then_is_yesterday_returns_true()
        => TimeSpan.FromDays(1).IsYesterday().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_two_days_then_is_yesterday_returns_false()
        => TimeSpan.FromDays(3).IsYesterday().ShouldBeFalse();
}
