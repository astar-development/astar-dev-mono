namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;

public interface IOneDriveClientOptions
{
    string AzureClientId { get; init; }
    Uri RedirectUri { get; init; }
}
