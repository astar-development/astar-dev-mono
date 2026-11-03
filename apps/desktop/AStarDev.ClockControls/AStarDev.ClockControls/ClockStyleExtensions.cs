namespace AStarDev.ClockControls;

/// <summary>Conversions for <see cref="ClockStyle"/>.</summary>
public static class ClockStyleExtensions
{
    /// <summary>Returns the key used to persist the style.</summary>
    public static string ToKey(this ClockStyle style) => style is DigitalClockStyle ? "Digital" : "Analogue";
}
