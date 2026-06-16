namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

public record ClientConfiguration(string ApplicationName, string ApplicationVersion)
{
    public ClientConfiguration() : this(string.Empty, string.Empty)
    {

    }

    internal static string SectionName => "AStarDevOneDriveClient";
}
