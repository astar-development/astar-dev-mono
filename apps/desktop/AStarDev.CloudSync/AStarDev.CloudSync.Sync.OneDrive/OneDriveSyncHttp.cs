using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>The HTTP names the OneDrive sync adapters share.</summary>
public static class OneDriveSyncHttp
{
    /// <summary>The named <see cref="HttpClient"/> whose pipeline includes the account throttle gate. Graph, download-URL and upload-URL requests all go through it.</summary>
    public const string ClientName = "OneDriveSync";

    /// <summary>The request option that names the account a request is for; requests without it bypass the throttle gate.</summary>
    public static readonly HttpRequestOptionsKey<AccountId> AccountOption = new("AStarDev.CloudSync.AccountId");
}
