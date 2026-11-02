using System.Globalization;

namespace AStarDev.ClockControls.TestsUnit;

public sealed class GivenADigitalClockFormatter
{
    private static readonly DateTime Time = new(2026, 11, 2, 9, 5, 7);

    [Fact]
    public void when_seconds_are_shown_then_hours_minutes_and_seconds_are_formatted()
    {
        string result = DigitalClockFormatter.Format(Time, true, CultureInfo.InvariantCulture);

        result.ShouldBe("09:05:07");
    }

    [Fact]
    public void when_seconds_are_hidden_then_only_hours_and_minutes_are_formatted()
    {
        string result = DigitalClockFormatter.Format(Time, false, CultureInfo.InvariantCulture);

        result.ShouldBe("09:05");
    }

    [Fact]
    public void when_the_culture_uses_a_twelve_hour_clock_then_the_designator_is_included()
    {
        string result = DigitalClockFormatter.Format(Time, false, CultureInfo.GetCultureInfo("en-US"));

        result.ShouldContain("AM");
    }
}
