using System.IO.Abstractions;
using AStar.Dev.Logging.Extensions;
using AStarDev.Utilities;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
/// The <see cref="ApplicationDirectories" /> class provides access to the application's data, cache, and log directories, ensuring they exist and are accessible.
/// </summary>
/// <param name="fileSystem">The file system abstraction used to interact with the file system.</param>
/// <param name="logger">The logger used for logging directory creation and access.</param>
public class ApplicationDirectories(IFileSystem fileSystem, ILogger<ApplicationDirectories> logger) : IApplicationDirectories
{
    private static readonly string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationFolder);

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDirectories" /> class, ensuring that the necessary application directories exist.
    /// </summary>
    public void CreateIfRequired()
    {
        LogMessage.Debug(logger, "Ensuring application directories exist at {Root}", root);
        fileSystem.Directory.CreateDirectory(DataDirectory);
        fileSystem.Directory.CreateDirectory(LogsDirectory);
        fileSystem.Directory.CreateDirectory(CacheDirectory);
        fileSystem.Directory.CreateDirectory(DocumentsExportDirectory);
    }

    /// <summary>
    /// Gets the root directory for application data, typically located in the user's application data folder.
    /// </summary>
    public static string DataDirectory => root.CombinePath("data");

    /// <summary>
    /// Gets the directory for cached data, typically located in the user's application data folder.
    /// </summary>
    public static string CacheDirectory => root.CombinePath("cache");

    /// <summary>
    /// Gets the directory for log files, typically located in the user's application data folder.
    /// </summary>
    public static string LogsDirectory => root.CombinePath("logs");

    /// <summary>
    /// The user's documents folder where table Import/Export JSON files are read from and written to.
    /// </summary>
    public static string DocumentsExportDirectory => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath(ApplicationMetadata.ApplicationFolder);
}
