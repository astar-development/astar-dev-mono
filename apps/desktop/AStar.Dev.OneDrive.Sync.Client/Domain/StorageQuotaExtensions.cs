namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Extension methods for <see cref="StorageQuota"/>.</summary>
public static class StorageQuotaExtensions
{
    /// <summary>Returns the fraction of storage used, clamped to [0, 1]. Returns 0 when <see cref="StorageQuota.TotalBytes"/> is 0.</summary>
    public static double Fraction(this StorageQuota quota) => quota.TotalBytes > 0 ? Math.Clamp((double)quota.UsedBytes / quota.TotalBytes, 0, 1) : 0;
}
