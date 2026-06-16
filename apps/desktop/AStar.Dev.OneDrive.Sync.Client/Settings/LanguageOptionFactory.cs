using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public static class LanguageOptionFactory
{
    /// <summary>Creates one <see cref="LanguageOption"/> per culture discovered by the localisation service, with <see cref="LanguageOption.IsSelected"/> set for the current culture.</summary>
    public static IReadOnlyList<LanguageOption> Create(ILocalizationService loc) =>
        loc.AvailableCultures.Select(culture => new LanguageOption(culture, culture.NativeName, culture.Equals(loc.CurrentCulture))).ToList();
}
