using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Classification;

/// <summary>Registers the classification module.</summary>
public static class ClassificationServiceCollectionExtensions
{
    /// <summary>Registers the classification engine, vocabulary cache, seeding, suggestions, reclassify and the sync hook. Register the sync persistence and then the classification persistence first: the term and category stores and the baseline writer registered before this call are wrapped, never replaced.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddClassification(this IServiceCollection services) => throw new NotImplementedException($"{nameof(services)} {services}");
}
