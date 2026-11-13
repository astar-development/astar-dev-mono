namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>The drive and the access token for one OneDrive call. Never logged.</summary>
internal sealed record TransferCredentials
{
    internal TransferCredentials(string driveId, string accessToken)
    {
        DriveId = driveId;
        AccessToken = accessToken;
    }

    internal string DriveId { get; }

    internal string AccessToken { get; }

    public override string ToString() => "TransferCredentials(redacted)";
}
