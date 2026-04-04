using AStar.Dev.Conflict.Resolution.Features.Detection;
using AStar.Dev.Conflict.Resolution.Features.Resolution;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Conflict.Resolution;

/// <summary>Registers conflict resolution services into the DI container.</summary>
public static class ConflictResolutionServiceExtensions
{
    /// <summary>
    ///     Adds <see cref="IConflictDetector"/>, <see cref="IConflictResolver"/>, and <see cref="ICascadeService"/>
    ///     as singletons. The caller must register <see cref="Features.Persistence.IConflictStore"/>
    ///     separately (implementation lives in the desktop app — requires EF Core).
    /// </summary>
    public static IServiceCollection AddConflictResolution(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IConflictDetector, ConflictDetector>();
        services.AddSingleton<IConflictResolver, ConflictResolver>();
        services.AddSingleton<ICascadeService, CascadeService>();

        return services;
    }
}
