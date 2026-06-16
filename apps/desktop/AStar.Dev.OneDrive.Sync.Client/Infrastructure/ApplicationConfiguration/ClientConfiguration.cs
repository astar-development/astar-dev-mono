namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

public record ClientConfiguration(string ApplicationName, string ApplicationVersion)
{
    internal static string SectionName => "AStarDevOneDriveClient";
}
