using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public sealed record LanguageOption(CultureInfo Culture, string Label, bool IsSelected = false);
