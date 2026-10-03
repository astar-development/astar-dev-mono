using System.Diagnostics.CodeAnalysis;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Decides what happens when the person picks another configuration in the editor.</summary>
public static class ConfigurationSwitch
{
    /// <summary>Confirms the edits can be discarded, then loads the requested configuration.</summary>
    /// <param name="requested">The configuration the person picked.</param>
    /// <param name="canDiscardEdits">Asks whether the unsaved edits may be discarded.</param>
    /// <param name="session">How to load the requested configuration.</param>
    /// <returns>The loaded configuration, or none when the picker must go back to the configuration being edited, with the failure to show when there is one.</returns>
    public static async Task<ConfigurationSwitchResult> ResolveAsync(ScrapeConfigurationSummary requested, Func<Task<bool>> canDiscardEdits, ConfigurationEditorSession session)
    {
        var discard = await ConfirmDiscardAsync(canDiscardEdits);
        if (discard.Failure.TryGetValue(out var failure)) return ConfigurationSwitchResult.Failed($"Unable to confirm discarding the unsaved edits. {failure}");

        return discard.Confirmed ? await LoadAsync(requested, session) : ConfigurationSwitchResult.Stay;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void handler: any exception escaping it would crash the application, so the failure is shown in the editor instead.")]
    private static async Task<ConfigurationSwitchResult> LoadAsync(ScrapeConfigurationSummary requested, ConfigurationEditorSession session)
    {
        try
        {
            return ConfigurationSwitchResult.Switched(await session.LoadAsync(requested));
        }
        catch (Exception exception)
        {
            return ConfigurationSwitchResult.Failed($"Unable to load the scrape configuration. {exception.Message}");
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void handler: any exception escaping it would crash the application, so the failure is shown in the editor instead.")]
    private static async Task<DiscardAnswer> ConfirmDiscardAsync(Func<Task<bool>> canDiscardEdits)
    {
        try
        {
            return new DiscardAnswer(await canDiscardEdits(), Option.None<string>());
        }
        catch (Exception exception)
        {
            return new DiscardAnswer(false, Option.Some(exception.Message));
        }
    }

    private readonly record struct DiscardAnswer(bool Confirmed, Option<string> Failure);
}

/// <summary>The outcome of picking another configuration in the editor.</summary>
/// <param name="Loaded">The configuration to show, or none when the picker must go back to the configuration being edited.</param>
/// <param name="Error">The failure to show, or none when there is none.</param>
public sealed record ConfigurationSwitchResult(Option<LoadedConfiguration> Loaded, Option<string> Error)
{
    /// <summary>Stay on the configuration being edited, with nothing to report.</summary>
    public static ConfigurationSwitchResult Stay { get; } = new(Option.None<LoadedConfiguration>(), Option.None<string>());

    /// <summary>Show the configuration that was loaded, or go back when it was not found.</summary>
    /// <param name="loaded">The configuration that was loaded.</param>
    /// <returns>The outcome, with nothing to report.</returns>
    public static ConfigurationSwitchResult Switched(Option<LoadedConfiguration> loaded) => new(loaded, Option.None<string>());

    /// <summary>Go back to the configuration being edited and show why.</summary>
    /// <param name="message">The failure to show.</param>
    /// <returns>The outcome, with the failure to report.</returns>
    public static ConfigurationSwitchResult Failed(string message) => new(Option.None<LoadedConfiguration>(), Option.Some(message));
}
