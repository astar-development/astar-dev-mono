using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client.Localization;

/// <summary>
/// Provides localised string lookup with optional format-argument support.
/// Implementations load strings from JSON resource files keyed by culture.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Current active culture (e.g. en-GB, fr-FR).</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>All cultures for which a resource file exists.</summary>
    IReadOnlyList<CultureInfo> AvailableCultures { get; }

    /// <summary>
    /// Returns the localised string for <paramref name="key"/>.
    /// Falls back to the key itself if not found so the UI never shows blank.
    /// </summary>
    string GetLocal(string key);

    /// <summary>
    /// Returns the localised string for <paramref name="key"/> with
    /// <see cref="string.Format(string, object[])"/> applied to <paramref name="args"/>.
    /// </summary>
    string GetLocal(string key, params object[] args);

    /// <summary>
    /// Switches the active culture and reloads strings.
    /// Raises <see cref="CultureChanged"/>.
    /// </summary>
    Task SetCultureAsync(CultureInfo culture);

    /// <summary>Raised after a successful culture switch.</summary>
    event EventHandler<CultureInfo>? CultureChanged;

    /// <summary>
    /// Must be called once at startup (e.g. from App.OnFrameworkInitializationCompleted).
    /// Loads strings for the requested culture (or en-GB as fallback).
    /// </summary>
    void Initialise(CultureInfo? requested = null);
}
