namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Provides extension methods for the <see cref="StorageQuota"/> record, allowing for additional functionality such as calculating the fraction of storage used. This class serves as a utility to enhance the capabilities of the StorageQuota entity without modifying its original structure, adhering to the open/closed principle and promoting separation of concerns within the codebase.
/// </summary>
public static class StorageQuotaExtensions
{
    /// <summary>
    /// Calculates the fraction of storage used based on the total and used bytes in the <see cref="StorageQuota"/>. This method returns a value between 0 and 1, representing the percentage of storage used. If the total storage is zero, it returns 0 to avoid division by zero errors. This can be useful for displaying storage usage in a user interface or for making decisions based on storage capacity within the sync client application.
    /// </summary>
    /// <param name="quota">The storage quota for which to calculate the fraction used.</param>
    /// <returns>The fraction of storage used, clamped to [0, 1].</returns>
    public static double Fraction(this StorageQuota quota) => quota.TotalBytes > 0 ? Math.Clamp((double)quota.UsedBytes / quota.TotalBytes, 0, 1) : 0;
}
