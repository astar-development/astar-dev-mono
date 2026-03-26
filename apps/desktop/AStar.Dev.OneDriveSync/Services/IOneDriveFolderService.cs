using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Services;

/// <summary>AM-03: Lists OneDrive folders via the Microsoft Graph API.</summary>
public interface IOneDriveFolderService
{
    Task<Result<IReadOnlyList<OneDriveFolder>, string>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OneDriveFolder>, string>> GetChildFoldersAsync(string accessToken, string folderId, CancellationToken ct = default);
}
