namespace AStar.Dev.Clock.Controls;

/// <summary>Computes clock hand angle units (0.0-1.0 turns) from a point in time.</summary>
public static class ClockHandGeometry
{
    /// <summary>Gets the second hand's angle unit, snapped to whole seconds so it always lands on a tick mark.</summary>
    public static double SecondHandUnit(DateTime now) => now.Second / 60.0;
}
