#nullable enable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;

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
