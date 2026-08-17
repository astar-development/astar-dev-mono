using Avalonia.Media;

namespace AStar.Dev.Clock.Controls;

/// <summary>Defines the stroke thickness and brush used to draw each clock hand.</summary>
public static class ClockHandStyle
{
    /// <summary>Gets the hour hand's stroke thickness.</summary>
    public const double HourHandThickness = 5;

    /// <summary>Gets the minute hand's stroke thickness.</summary>
    public const double MinuteHandThickness = 4;

    /// <summary>Gets the second hand's stroke thickness.</summary>
    public const double SecondHandThickness = 1.5;

    /// <summary>Gets the hour hand's brush.</summary>
    public static readonly IBrush HourHandBrush = Brushes.Red;

    /// <summary>Gets the minute hand's brush: a vivid blue that stays visually distinguishable as blue under a warm colour-temperature shift such as Windows Night Light.</summary>
    public static readonly IBrush MinuteHandBrush = new SolidColorBrush(Color.FromUInt32(0xFF2979FF));
}
