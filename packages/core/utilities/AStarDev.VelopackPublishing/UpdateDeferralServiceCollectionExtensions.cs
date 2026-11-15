using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>Registers the update deferral store.</summary>
public static class UpdateDeferralServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="IUpdateDeferralStore"/> that keeps the deferral in a JSON file.</summary>
    /// <param name="services">The service collection to register the store with.</param>
    /// <param name="deferralFile">Returns the JSON file the deferral is kept in.</param>
    /// <returns>The <paramref name="services"/> collection to allow further chaining.</returns>
    public static IServiceCollection AddVelopackUpdateDeferral(this IServiceCollection services, Func<IServiceProvider, IFileInfo> deferralFile) =>
        services.AddSingleton<IUpdateDeferralStore>(provider => new JsonUpdateDeferralStore(deferralFile(provider), provider.GetRequiredService<ILogger<JsonUpdateDeferralStore>>()));
}
