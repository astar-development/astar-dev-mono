using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>A scrape configuration read into the inputs the editor shows, so the editor never sees the stored entity.</summary>
/// <param name="Id">The identity the edits are saved against.</param>
/// <param name="Inputs">The contents of every editor section.</param>
public sealed record LoadedConfiguration(ScrapeConfigurationId Id, ConfigurationEditInputs Inputs)
{
    /// <summary>Reads <paramref name="entity"/> for editing.</summary>
    /// <param name="entity">The scrape configuration to edit.</param>
    public static LoadedConfiguration From(ScrapeConfigurationEntity entity) => new(entity.Id, ConfigurationEditInputs.From(entity));
}
