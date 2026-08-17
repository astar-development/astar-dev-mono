namespace AStarDev.OneDriveSyncClient.Infrastructure.OneDrive;

public interface IOneDriveClientOptions
{
    string AzureClientId { get; init; }
    Uri RedirectUri { get; init; }
}
