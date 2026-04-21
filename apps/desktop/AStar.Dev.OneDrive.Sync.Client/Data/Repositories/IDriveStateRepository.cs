using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface IDriveStateRepository
{
    /// <summary>Returns the drive state for the specified account, or null if none exists.</summary>
    Task<DriveStateEntity?> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the drive state for the specified account.</summary>
    Task UpsertAsync(DriveStateEntity driveState, CancellationToken cancellationToken);

    /// <summary>Clears the delta link for the specified account, forcing a full re-enumeration on next sync.</summary>
    Task ClearDeltaLinkAsync(AccountId accountId, CancellationToken cancellationToken);
}
