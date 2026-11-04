using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="CloudProvider"/> cases.</summary>
public static class CloudProviderFactory
{
    /// <summary>Creates the OneDrive provider.</summary>
    public static CloudProvider CreateOneDrive() => new CloudProvider.OneDrive();
}
