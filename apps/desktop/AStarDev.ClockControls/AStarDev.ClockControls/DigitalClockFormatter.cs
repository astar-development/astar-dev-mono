using System.Globalization;

namespace AStarDev.ClockControls;

/// <summary>Formats a time for the digital clock.</summary>
public static class DigitalClockFormatter
{
    /// <summary>Formats <paramref name="time"/> using the culture's long time pattern when <paramref name="showSeconds"/> is set, otherwise its short time pattern.</summary>
    public static string Format(DateTime time, bool showSeconds, CultureInfo culture) => time.ToString(showSeconds ? "T" : "t", culture);
}
