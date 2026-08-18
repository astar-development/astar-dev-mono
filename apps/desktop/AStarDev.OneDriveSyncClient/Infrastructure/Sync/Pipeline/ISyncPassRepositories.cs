using AStarDev.OneDriveSyncClient.Data.Repositories;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

/// <summary>
///   Wraps the three repositories used in a sync pass into a single object for DI.
/// </summary>
public interface ISyncPassRepositories
{
    /// <summary>
    ///  Gets the account repository used in a sync pass.
    /// </summary>
    IAccountRepository AccountRepository { get; }

    /// <summary>
    ///  Gets the drive state repository used in a sync pass.
    /// </summary>
    IDriveStateRepository DriveStateRepository { get; }

    /// <summary>
    ///  Gets the file classification repository used in a sync pass.
    /// </summary>
    IFileClassificationRepository ClassificationRepository { get; }
}
