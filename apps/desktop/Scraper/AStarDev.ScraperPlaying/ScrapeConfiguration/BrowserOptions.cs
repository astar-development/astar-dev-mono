using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>How the scraping browser is run.</summary>
/// <param name="UseHeadless">Whether the browser runs without a visible window.</param>
/// <param name="SlowMotionDelay">The delay in milliseconds between browser actions, none for no delay.</param>
public sealed record BrowserOptions(bool UseHeadless, Option<float> SlowMotionDelay)
{
    /// <summary>Copies the browser options from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static BrowserOptions From(ScrapeConfigurationEntity entity) => new(entity.UseHeadless, entity.SlowMotionDelay is { } delay ? Option.Some(delay) : Option.None<float>());

    /// <summary>Replaces the browser options of the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to update.</param>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        entity.UseHeadless = UseHeadless;
        entity.SlowMotionDelay = SlowMotionDelay.Match(delay => (float?)delay, () => null);
    }

    /// <summary>Adds an error when the slow motion delay is negative.</summary>
    /// <param name="errors">The collection the errors are added to.</param>
    internal void Validate(List<ValidationError> errors) => _ = SlowMotionDelay.Match(delay => RootSettingsInput.RequireNotNegative(nameof(SlowMotionDelay), delay, errors), () => true);
}
