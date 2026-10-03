using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>The status messages for the outcomes of importing and exporting a scrape configuration.</summary>
public static class ConfigurationTransferMessages
{
    /// <summary>Gets the message for an import outcome.</summary>
    /// <param name="outcome">Some when the import completed, none when the user did not pick a file.</param>
    public static string ForImport(Option<Unit> outcome) => outcome.Match(
        _ => "Scrape configuration imported.",
        () => "Scrape configuration import cancelled.");

    /// <summary>Gets the message for an export outcome.</summary>
    /// <param name="outcome">Some(true) when a configuration was written, some(false) when there was none to export, none when the user did not pick a file.</param>
    /// <param name="apiKeys">Whether the file was asked to carry the API keys.</param>
    public static string ForExport(Option<bool> outcome, ApiKeyExport apiKeys) => outcome.Match(
        exported => exported ? $"Scrape configuration exported {(apiKeys == ApiKeyExport.Include ? "including its API keys" : "without its API keys")}." : "No scrape configuration was found to export.",
        () => "Scrape configuration export cancelled.");
}
