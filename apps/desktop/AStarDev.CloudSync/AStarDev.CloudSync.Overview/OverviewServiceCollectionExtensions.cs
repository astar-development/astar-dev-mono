using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Overview;

/// <summary>Registers the overview module.</summary>
public static class OverviewServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="ISyncOverviewSource" /> that builds the overview from the saved accounts. Needs an <c>IAccountStore</c>, an <c>ICloudSyncDatabase</c>, an <c>IActivityLogReader</c>, an <c>ISyncAttentionReader</c>, an <c>ISyncBaselineCounter</c> and an <c>IAccountThrottleHints</c>, and uses the registered <c>IThroughputReader</c> or one that finds no samples.</summary>
    /// <param name="services">The service collection to register the overview source with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddSyncOverview(this IServiceCollection services) => AddTelemetry(services).AddSingleton<SyncOverviewReaders>().AddSingleton<OverviewThroughput>().AddSingleton<OverviewConflicts>().AddSingleton<OverviewPairs>().AddSingleton<AccountStoreSyncOverviewSource>().AddSingleton<ThrottleHintingSyncOverviewSource>().AddSingleton<ISyncOverviewSource, InstrumentedSyncOverviewSource>();

    private static IServiceCollection AddTelemetry(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IThroughputReader, NoThroughputReader>();
        services.TryAddSingleton<IWorkflowTelemetry, WorkflowTelemetry>();

        return services;
    }
}
