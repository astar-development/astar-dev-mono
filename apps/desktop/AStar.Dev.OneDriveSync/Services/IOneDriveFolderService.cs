namespace AStar.Dev.OneDriveSync.Services;

/// <summary>AM-03: Lists OneDrive folders via the Microsoft Graph API.</summary>
public interface IOneDriveFolderService
{
    Task<IReadOnlyList<OneDriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default);
    Task<IReadOnlyList<OneDriveFolder>> GetChildFoldersAsync(string accessToken, string folderId, CancellationToken ct = default);
}
