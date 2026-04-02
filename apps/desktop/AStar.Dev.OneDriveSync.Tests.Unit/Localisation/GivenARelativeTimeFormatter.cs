using AStar.Dev.OneDriveSync.Infrastructure.Localisation;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Localisation;

public sealed class GivenARelativeTimeFormatter
{
    private readonly ILocalisationService _localisationService = Substitute.For<ILocalisationService>();
    private readonly RelativeTimeFormatter _sut;

    public GivenARelativeTimeFormatter()
    {
        _localisationService.CurrentLocale.Returns("en-GB");
        _localisationService.GetString("RelativeTimeFormatter_OneMinuteAgo").Returns("1 minute ago");
        _localisationService.GetString("RelativeTimeFormatter_MinutesAgo").Returns("{0} minutes ago");
        _localisationService.GetString("RelativeTimeFormatter_TodayAt").Returns("Today at ");
        _localisationService.GetString("RelativeTimeFormatter_DateAt").Returns(" at ");
        _sut = new RelativeTimeFormatter(_localisationService);
    }

    [Fact]
    public void when_difference_is_less_than_one_minute_then_returns_one_minute_ago()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddSeconds(-30);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("1 minute ago");
    }

    [Fact]
    public void when_difference_is_exactly_five_minutes_then_returns_five_minutes_ago()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddMinutes(-5);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("5 minutes ago");
    }

    [Fact]
    public void when_difference_is_fifty_nine_minutes_then_returns_relative_string()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddMinutes(-59);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("59 minutes ago");
    }

    [Fact]
    public void when_difference_is_exactly_one_hour_then_returns_absolute_today_string()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddHours(-1);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("Today at 11:00");
    }

    [Fact]
    public void when_difference_is_more_than_one_hour_same_day_then_returns_today_format()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 14, 32, 0, TimeSpan.Zero);
        var timestamp = new DateTimeOffset(2026, 3, 31, 9, 15, 0, TimeSpan.Zero);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("Today at 09:15");
    }

    [Fact]
    public void when_timestamp_is_on_a_different_day_then_returns_date_format()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 14, 32, 0, TimeSpan.Zero);
        var timestamp = new DateTimeOffset(2026, 3, 25, 9, 15, 0, TimeSpan.Zero);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("25 Mar at 09:15");
    }

    [Fact]
    public void when_timestamp_is_in_the_future_then_returns_one_minute_ago()
    {
        var now       = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddMinutes(5);

        var result = _sut.Format(timestamp, now);

        result.ShouldBe("1 minute ago");
    }
}
