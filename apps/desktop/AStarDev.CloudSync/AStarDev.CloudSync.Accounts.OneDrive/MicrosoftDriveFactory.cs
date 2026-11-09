using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Creates <see cref="MicrosoftDrive"/> values.</summary>
internal static class MicrosoftDriveFactory
{
    /// <summary>Creates a drive from its id and storage.</summary>
    /// <param name="driveId">The drive's id.</param>
    /// <param name="storage">The drive's used and total storage.</param>
    public static MicrosoftDrive Create(DriveId driveId, StorageUsage storage) => new(driveId, storage);
}
