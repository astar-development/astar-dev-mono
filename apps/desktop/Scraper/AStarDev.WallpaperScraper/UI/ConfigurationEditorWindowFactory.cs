using System.IO.Abstractions;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Creates the editor window for a loaded scrape configuration.</summary>
/// <param name="saver">The service the editor validates and saves through.</param>
/// <param name="fileSystem">The file system the editor uses to check the scrape directories.</param>
public sealed class ConfigurationEditorWindowFactory(ConfigurationEditSaver saver, IFileSystem fileSystem)
{
    /// <summary>Creates the editor opened on the session's initial configuration.</summary>
    /// <param name="session">The configuration to edit first and how to switch to the others.</param>
    public ConfigurationEditorWindow Create(ConfigurationEditorSession session) => new(session, saver, fileSystem);
}
