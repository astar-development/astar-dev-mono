using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.SingleInstance;

/// <summary>Registers the single-instance guard.</summary>
public static class SingleInstanceServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ISingleInstanceGuard"/> as a singleton. Needs logging.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services"/> collection to allow further chaining.</returns>
    public static IServiceCollection AddSingleInstance(this IServiceCollection services) => services.AddSingleton<ISingleInstanceGuard, SingleInstanceGuard>();
}
