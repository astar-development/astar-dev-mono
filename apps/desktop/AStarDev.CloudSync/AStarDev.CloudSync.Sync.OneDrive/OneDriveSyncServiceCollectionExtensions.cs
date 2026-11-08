using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Registers the OneDrive sync module.</summary>
public static class OneDriveSyncServiceCollectionExtensions
{
    /// <summary>Registers the OneDrive sync adapters. Nothing is registered until the first adapter lands (#1396); the host already calls this so later issues only add registrations.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddOneDriveSync(this IServiceCollection services) => throw new NotImplementedException($"{services}");
}
