using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>The OneDrive drive of a signed-in account.</summary>
/// <param name="DriveId">The drive's id.</param>
/// <param name="Storage">The drive's used and total storage.</param>
public sealed record MicrosoftDrive(DriveId DriveId, StorageUsage Storage);
