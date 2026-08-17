namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Factory class for creating instances of <see cref="StorageQuota"/>. This class provides methods to create StorageQuota objects from raw byte counts, as well as a predefined "Unknown" quota for cases where storage information is not yet available. By centralizing the creation logic for StorageQuota instances, this factory promotes consistency and maintainability in how storage quota data is handled throughout the sync client application.
/// </summary>
public static class StorageQuotaFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="StorageQuota"/> with the specified total and used byte counts. This method allows for the creation of StorageQuota objects based on raw data, which can be useful when processing responses from the Graph API or when calculating storage usage based on local file information. By providing a factory method for creating StorageQuota instances, we can ensure that all StorageQuota objects are created in a consistent manner, making it easier to manage and utilize storage quota information within the sync client application.
    /// </summary>
    /// <param name="totalBytes">The total storage capacity in bytes.</param>
    /// <param name="usedBytes">The amount of storage currently used in bytes.</param>
    /// <returns>A new instance of <see cref="StorageQuota"/>.</returns>
    public static StorageQuota Create(long totalBytes, long usedBytes) => new(totalBytes, usedBytes);

    /// <summary>
    /// Provides a predefined instance of <see cref="StorageQuota"/> representing an unknown storage state, where both total and used bytes are set to zero. This can be used as a default value when storage information is not yet available or cannot be retrieved, allowing the sync client application to handle such scenarios gracefully without encountering null references or uninitialized data. By having a standard "Unknown" quota, we can improve the robustness of the application when dealing with storage information that may be incomplete or unavailable.
    /// </summary>
    public static StorageQuota Unknown => new(0, 0);
}
