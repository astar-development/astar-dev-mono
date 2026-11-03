using AStarDev.ClockControls;

namespace AStarDev.OneDriveSyncClient.Settings;

/// <summary>A selectable clock style shown in Settings.</summary>
/// <param name="Style">The style the option selects.</param>
/// <param name="Label">Localised label.</param>
/// <param name="IsSelected">Whether the option is the current style.</param>
public sealed record ClockStyleOption(ClockStyle Style, string Label, bool IsSelected);
