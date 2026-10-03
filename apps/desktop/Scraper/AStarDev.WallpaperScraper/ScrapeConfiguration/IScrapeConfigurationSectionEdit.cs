using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>A validated edit to one section of a scrape configuration, applied when the editor is saved.</summary>
public interface IScrapeConfigurationSectionEdit
{
    /// <summary>Applies this edit to the specified scrape configuration.</summary>
    /// <param name="entity">The tracked scrape configuration to change.</param>
    /// <param name="now">The time to record as the modified time of anything this edit changes.</param>
    void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now);
}
