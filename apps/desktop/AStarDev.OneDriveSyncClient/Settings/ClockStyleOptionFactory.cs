using AStarDev.ClockControls;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Settings;

/// <summary>Creates <see cref="ClockStyleOption"/> lists.</summary>
public static class ClockStyleOptionFactory
{
    /// <summary>Creates the localised analogue and digital options with the one matching <paramref name="selected"/> marked selected.</summary>
    public static IReadOnlyList<ClockStyleOption> Create(ILocalizationService loc, ClockStyle selected) =>
    [
        new(ClockStyleFactory.Analogue(), loc.GetLocal("Settings.Clock.Analogue"), selected is AnalogueClockStyle),
        new(ClockStyleFactory.Digital(), loc.GetLocal("Settings.Clock.Digital"), selected is DigitalClockStyle),
    ];
}
