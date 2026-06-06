namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

public record ClientConfiguration
{
    public required string ApplicationName { get; init; }
    public required string ApplicationVersion { get; init; }
}
