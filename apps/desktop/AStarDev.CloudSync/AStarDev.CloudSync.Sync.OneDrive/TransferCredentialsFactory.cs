namespace AStarDev.CloudSync.Sync.OneDrive;

internal static class TransferCredentialsFactory
{
    internal static TransferCredentials Create(string driveId, string accessToken) => new(driveId, accessToken);
}
