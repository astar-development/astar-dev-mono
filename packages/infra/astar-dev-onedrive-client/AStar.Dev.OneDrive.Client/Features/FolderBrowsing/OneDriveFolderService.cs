using AStar.Dev.Functional.Extensions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Client.Features.FolderBrowsing;

/// <summary>
///     Graph API implementation of <see cref="IOneDriveFolderService"/> (AM-03, AM-04).
/// </summary>
internal sealed class OneDriveFolderService : IOneDriveFolderService
{
    public async Task<Result<IReadOnlyList<OneDriveFolder>, string>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        try
        {
            using var client = BuildGraphClient(accessToken);
            var drive = await client.Me.Drive.GetAsync(cancellationToken: ct).ConfigureAwait(false);

            if (drive?.Id is null)
                return new Result<IReadOnlyList<OneDriveFolder>, string>.Error("Could not resolve OneDrive ID for account");

            var response = await client.Drives[drive.Id].Items["root"].Children.GetAsync(config =>
            {
                config.QueryParameters.Filter = "folder ne null";
                config.QueryParameters.Select = ["id", "name", "parentReference", "folder"];
            }, ct).ConfigureAwait(false);

            return new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(MapFolders(response?.Value, parentId: null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<IReadOnlyList<OneDriveFolder>, string>.Error($"Failed to fetch root folders: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<OneDriveFolder>, string>> GetChildFoldersAsync(string accessToken, string folderId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        try
        {
            using var client = BuildGraphClient(accessToken);
            var drive = await client.Me.Drive.GetAsync(cancellationToken: ct).ConfigureAwait(false);

            if (drive?.Id is null)
                return new Result<IReadOnlyList<OneDriveFolder>, string>.Error("Could not resolve OneDrive ID for account");

            var response = await client.Drives[drive.Id].Items[folderId].Children.GetAsync(config =>
            {
                config.QueryParameters.Filter = "folder ne null";
                config.QueryParameters.Select = ["id", "name", "parentReference", "folder"];
            }, ct).ConfigureAwait(false);

            return new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(MapFolders(response?.Value, parentId: folderId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<IReadOnlyList<OneDriveFolder>, string>.Error($"Failed to fetch child folders for '{folderId}': {ex.Message}");
        }
    }

    private static GraphServiceClient BuildGraphClient(string accessToken)
    {
        var tokenProvider = new StaticAccessTokenProvider(accessToken);
        var authProvider  = new BaseBearerTokenAuthenticationProvider(tokenProvider);

        return new GraphServiceClient(authProvider);
    }

    private static IReadOnlyList<OneDriveFolder> MapFolders(IList<DriveItem>? items, string? parentId)
    {
        if (items is null or { Count: 0 })
            return [];

        return [.. items
            .Where(item => item.Folder is not null && item.Id is not null && item.Name is not null)
            .Select(item => OneDriveFolderFactory.Create(
                id:          item.Id!,
                name:        item.Name!,
                parentId:    parentId,
                hasChildren: (item.Folder!.ChildCount ?? 0) > 0))];
    }

    private sealed class StaticAccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(accessToken);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
