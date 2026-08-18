using AStarDev.OneDriveSyncClient.Data.Repositories;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

/// <summary>
///    Wraps the three repositories used in a sync pass into a single object for DI.
/// </summary>
/// <param name="accountRepository"></param>
/// <param name="driveStateRepository"></param>
/// <param name="classificationRepository"></param>
internal sealed class SyncPassRepositories(IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, IFileClassificationRepository classificationRepository) : ISyncPassRepositories
{
    /// <inheritdoc />
    public IAccountRepository AccountRepository { get; } = accountRepository;

    /// <inheritdoc />
    public IDriveStateRepository DriveStateRepository { get; } = driveStateRepository;

    /// <inheritdoc />
    public IFileClassificationRepository ClassificationRepository { get; } = classificationRepository;
}
