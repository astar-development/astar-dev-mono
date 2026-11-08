using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Registers the SQLite sync module.</summary>
public static class SyncPersistenceServiceCollectionExtensions
{
    /// <summary>Registers the sync stores. Nothing is registered until the first sync table lands (#1397, #1398), which also brings the DbContext and its first migration; the host already calls this so later issues only add registrations.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddSyncPersistence(this IServiceCollection services) => throw new NotImplementedException($"{services}");
}
