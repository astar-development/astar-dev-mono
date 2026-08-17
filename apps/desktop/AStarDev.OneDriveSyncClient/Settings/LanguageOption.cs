using System.Globalization;

namespace AStarDev.OneDriveSyncClient.Settings;

public sealed record LanguageOption(CultureInfo Culture, string Label, bool IsSelected = false);
