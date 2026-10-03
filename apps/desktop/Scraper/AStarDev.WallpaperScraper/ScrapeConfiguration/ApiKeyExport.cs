namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Whether an exported configuration file carries the API keys. Exports leave them out unless the user asks for them, so a file that is shared or committed by mistake does not leak them.</summary>
public enum ApiKeyExport
{
    /// <summary>The API keys are left out of the file (written empty).</summary>
    Exclude,

    /// <summary>The API keys are written to the file.</summary>
    Include
}
