using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>Provides localisation support: string lookup, locale switching, and locale persistence (AC LO-01 to LO-04).</summary>
public interface ILocalisationService
{
    /// <summary>The BCP-47 locale code currently active (e.g. <c>en-GB</c>).</summary>
    string CurrentLocale { get; }

    /// <summary>All locales supported in the current build.</summary>
    IReadOnlyList<string> SupportedLocales { get; }

    /// <summary>Returns the localised string for <paramref name="key" />, or the key itself if not found.</summary>
    string GetString(string key);

    /// <summary>Applies and persists <paramref name="locale" /> as the active locale.</summary>
    Task<Result<string, ErrorResponse>> SetLocaleAsync(string locale, CancellationToken ct = default);

    /// <summary>Initialises the service from persisted settings; falls back to <c>en-GB</c> if none stored.</summary>
    Task InitialiseAsync(CancellationToken ct = default);

    /// <summary>Fired on the UI thread each time the active locale changes.</summary>
    event EventHandler? LocaleChanged;
}
