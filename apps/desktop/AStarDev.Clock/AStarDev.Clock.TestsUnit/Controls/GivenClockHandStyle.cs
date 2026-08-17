using Avalonia.Media;
using AStar.Dev.Clock.Controls;

namespace AStar.Dev.Clock.TestsUnit.Controls;

public sealed class GivenClockHandStyle
{
    [Fact]
    public void when_reading_the_minute_hand_thickness_then_it_is_thicker_than_the_second_hand()
    {
        ClockHandStyle.MinuteHandThickness.ShouldBeGreaterThan(ClockHandStyle.SecondHandThickness);
    }

    [Fact]
    public void when_reading_the_minute_hand_brush_then_it_is_a_vivid_blue_that_survives_warm_colour_shifting()
    {
        var color = ((ISolidColorBrush)ClockHandStyle.MinuteHandBrush).Color;

        color.B.ShouldBeGreaterThan(color.R);
        color.B.ShouldBeGreaterThan((byte)200);
    }
}
