namespace AStarDev.ClockControls;

/// <summary>How the clock is drawn.</summary>
public abstract record ClockStyle;

/// <summary>A round clock face with hands.</summary>
public sealed record AnalogueClockStyle : ClockStyle;

/// <summary>A numeric time readout.</summary>
public sealed record DigitalClockStyle : ClockStyle;
