using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.FolderBrowsing;

/// <summary>
///     Fetches OneDrive folder metadata from the Graph API (AM-03, AM-04).
///     All methods return <see cref="Result{TSuccess,TError}"/> — callers never see Graph exceptions.
/// </summary>
public interface IOneDriveFolderService
{
    /// <summary>
    ///     Returns the root-level folders for the authenticated user's OneDrive.
    ///     Call with the access token acquired by <c>IMsalClient.AcquireTokenInteractiveAsync</c>.
    /// </summary>
    Task<Result<IReadOnlyList<OneDriveFolder>, string>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    ///     Returns the direct child folders of the specified <paramref name="folderId"/>.
    ///     Used for lazy tree expansion (AM-03).
    /// </summary>
    Task<Result<IReadOnlyList<OneDriveFolder>, string>> GetChildFoldersAsync(string accessToken, string folderId, CancellationToken ct = default);
}
