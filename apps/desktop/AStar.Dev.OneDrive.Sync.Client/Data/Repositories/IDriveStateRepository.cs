using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface IDriveStateRepository
{
    /// <summary>Returns the drive state for the specified account, or <see cref="Option{T}.None"/> if none exists.</summary>
    Task<Option<DriveStateEntity>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the drive state for the specified account.</summary>
    Task UpsertAsync(DriveStateEntity driveState, CancellationToken cancellationToken);

    /// <summary>Clears the delta link for the specified account, forcing a full re-enumeration on next sync.</summary>
    Task ClearDeltaLinkAsync(AccountId accountId, CancellationToken cancellationToken);
}
