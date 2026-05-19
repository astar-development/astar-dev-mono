using AStar.Dev.OneDriveSyncClient.Infrastructure.Startup;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using LogMessage = AStar.Dev.Logging.Extensions.LogMessage;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Startup;

public class ApplicationDirectories(IFileSystem fileSystem, ILogger<ApplicationDirectories> logger) : IApplicationDirectories
{
    public void CreateIfRequired()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationFolder);

        LogMessage.Debug(logger, "Ensuring application directories exist at {Root}", root);
        fileSystem.Directory.CreateDirectory(DataDirectory);
        fileSystem.Directory.CreateDirectory(LogsDirectory);
        fileSystem.Directory.CreateDirectory(CacheDirectory);
    }

    public static string DataDirectory => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationFolder, "data");

    public static string CacheDirectory => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationFolder, "cache");

    public static string LogsDirectory => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationFolder, "logs");
}
