using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;

namespace AStarDev.ScraperPlaying.Startup;

/// <summary>Registers file system, database, and clock infrastructure services with the dependency injection container.</summary>
public static class InfrastructureServices
{
    /// <summary>Registers the file system abstraction, the database context factory, and the system clock delegate.</summary>
    /// <param name="services">The service collection to register the infrastructure services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();

        return services;
    }
}