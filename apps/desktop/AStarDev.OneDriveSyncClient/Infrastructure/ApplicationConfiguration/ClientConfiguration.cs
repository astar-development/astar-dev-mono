namespace AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;

public record ClientConfiguration
{
    internal static string SectionName => "AStarDevOneDriveClient";

    public required string ApplicationName { get; init; }
}
