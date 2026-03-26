using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDriveSync.Services;

/// <summary>AM-03: Lists OneDrive folders via the Microsoft Graph API.</summary>
public sealed class GraphOneDriveFolderService : IOneDriveFolderService
{
    public async Task<IReadOnlyList<OneDriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);
        var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
        var driveId = drive?.Id ?? throw new InvalidOperationException("Could not retrieve the user's default drive.");

        var root = await client.Drives[driveId].Root.GetAsync(cancellationToken: ct);
        var rootId = root?.Id ?? throw new InvalidOperationException("Could not retrieve the drive root item.");

        var children = await client.Drives[driveId].Items[rootId].Children.GetAsync(cancellationToken: ct);
        return MapDriveItems(children?.Value);
    }

    public async Task<IReadOnlyList<OneDriveFolder>> GetChildFoldersAsync(string accessToken, string folderId, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);
        var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
        var driveId = drive?.Id ?? throw new InvalidOperationException("Could not retrieve the user's default drive.");

        var children = await client.Drives[driveId].Items[folderId].Children.GetAsync(cancellationToken: ct);
        return MapDriveItems(children?.Value);
    }

    private static GraphServiceClient CreateClient(string accessToken) => new(new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken)));

    private static List<OneDriveFolder> MapDriveItems(List<DriveItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        return items.Where(i => i.Folder is not null).Select(i => new OneDriveFolder(i.Id ?? string.Empty, i.Name ?? string.Empty, (i.Folder?.ChildCount ?? 0) > 0)).ToList();
    }

    private sealed class StaticAccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default) => Task.FromResult(accessToken);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
