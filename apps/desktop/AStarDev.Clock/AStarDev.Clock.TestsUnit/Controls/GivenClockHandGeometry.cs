using AStar.Dev.Clock.Controls;

namespace AStar.Dev.Clock.TestsUnit.Controls;

public sealed class GivenClockHandGeometry
{
    [Fact]
    public void when_second_is_zero_then_second_hand_unit_is_zero()
    {
        var now = new DateTime(2026, 8, 2, 10, 12, 0, 0, DateTimeKind.Local);

        double unit = ClockHandGeometry.SecondHandUnit(now);

        unit.ShouldBe(0.0);
    }

    [Fact]
    public void when_second_is_fifteen_then_second_hand_unit_is_a_quarter_turn()
    {
        var now = new DateTime(2026, 8, 2, 10, 12, 15, 0, DateTimeKind.Local);

        double unit = ClockHandGeometry.SecondHandUnit(now);

        unit.ShouldBe(0.25);
    }

    [Fact]
    public void when_millisecond_is_non_zero_then_second_hand_unit_ignores_it()
    {
        var withoutMilliseconds = new DateTime(2026, 8, 2, 10, 12, 45, 0, DateTimeKind.Local);
        var withMilliseconds = new DateTime(2026, 8, 2, 10, 12, 45, 987, DateTimeKind.Local);

        double unit = ClockHandGeometry.SecondHandUnit(withMilliseconds);

        unit.ShouldBe(ClockHandGeometry.SecondHandUnit(withoutMilliseconds));
    }
}
