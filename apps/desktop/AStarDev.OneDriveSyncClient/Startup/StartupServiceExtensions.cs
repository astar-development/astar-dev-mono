using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.Startup;

/// <summary>Composition root for the sync client's startup tasks.</summary>
public static class StartupServiceExtensions
{
    /// <summary>Registers the sync client's startup tasks.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddStartupTasks(this IServiceCollection services) => services;
}
