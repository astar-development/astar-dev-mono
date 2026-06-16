namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

public record ClientConfiguration
{
    internal static string SectionName => "AStarDevOneDriveClient";

    public required string ApplicationName { get; init; }
    public required string ApplicationVersion { get; init; }
}
