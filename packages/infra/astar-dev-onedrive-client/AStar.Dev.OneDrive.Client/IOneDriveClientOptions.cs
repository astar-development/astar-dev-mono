namespace AStar.Dev.OneDrive.Client;

/// <summary>
///
/// </summary>
public interface IOneDriveClientOptions
{
    /// <summary>The Azure App Registration client ID for the consumers tenant.</summary>
    string AzureClientId { get; init; }
    /// <summary>Redirect URI registered in the Azure portal (local loopback or <c>urn:ietf:wg:oauth:2.0:oob</c>).</summary>
    Uri RedirectUri { get; init; }
}
