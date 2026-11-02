namespace AStarDev.ClockControls;

/// <summary>Creates <see cref="ClockStyle"/> cases.</summary>
public static class ClockStyleFactory
{
    private const string DigitalKey = "Digital";

    /// <summary>Creates the analogue style.</summary>
    public static ClockStyle Analogue() => new AnalogueClockStyle();

    /// <summary>Creates the digital style.</summary>
    public static ClockStyle Digital() => new DigitalClockStyle();

    /// <summary>Creates the style named by <paramref name="key"/>, ignoring case. A blank or unrecognised key gives the analogue style.</summary>
    public static ClockStyle Create(string? key) => string.Equals(key?.Trim(), DigitalKey, StringComparison.OrdinalIgnoreCase) ? Digital() : Analogue();
}
