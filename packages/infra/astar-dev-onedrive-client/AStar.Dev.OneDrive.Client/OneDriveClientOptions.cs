namespace AStar.Dev.OneDrive.Client;

/// <summary>
///     Configuration options for the OneDrive Client package.
/// </summary>
public sealed record OneDriveClientOptions : IOneDriveClientOptions
{
    /// <inheritdoc/>
    public required string AzureClientId { get; init; }

    /// <inheritdoc/>
    public required Uri RedirectUri { get; init; }
}
