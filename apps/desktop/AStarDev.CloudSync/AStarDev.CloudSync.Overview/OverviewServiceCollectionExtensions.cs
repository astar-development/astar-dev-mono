using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Overview;

/// <summary>Registers the overview module.</summary>
public static class OverviewServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="ISyncOverviewSource" /> that builds the overview from the saved accounts. Needs an <c>IAccountStore</c> and an <c>ICloudSyncDatabase</c>.</summary>
    /// <param name="services">The service collection to register the overview source with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddSyncOverview(this IServiceCollection services) => throw new NotImplementedException($"{services}");
}
