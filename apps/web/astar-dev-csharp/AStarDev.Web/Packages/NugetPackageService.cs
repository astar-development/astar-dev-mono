using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace AStarDev.Web.Packages;

/// <inheritdoc cref="INugetPackageService" />
public sealed class NugetPackageService(INugetApiClient apiClient, IMemoryCache cache, ILogger<NugetPackageService> logger) : INugetPackageService
{
    private static readonly TimeSpan FreshDuration = TimeSpan.FromHours(6);

    public async Task<Result<PackageData, string>> GetPackageDataAsync(string packageId, CancellationToken cancellationToken)
    {
        var freshKey = FreshCacheKeyFor(packageId);
        var lastGoodKey = LastGoodCacheKeyFor(packageId);

        if (TryGetCached(freshKey, out var freshValue))
            return freshValue;

        var fetchedOption = await apiClient.FetchAsync(packageId, cancellationToken);
        if (fetchedOption.TryGetValue(out var fetchedValue))
        {
            cache.Set(freshKey, fetchedValue, FreshDuration);
            cache.Set(lastGoodKey, fetchedValue);

            return fetchedValue;
        }

        if (TryGetCached(lastGoodKey, out var lastGoodValue))
        {
            LogMessage.Warning(logger, nameof(NugetPackageService), $"NuGet API unreachable for '{packageId}' — using last known good data.");

            return lastGoodValue;
        }

        LogMessage.Error(logger, $"NuGet API unreachable for '{packageId}' and no cached data is available.");

        return $"Package data for '{packageId}' is currently unavailable.";
    }

    private bool TryGetCached(string key, out PackageData value)
    {
        if (cache.TryGetValue(key, out PackageData? cached) && cached is not null)
        {
            value = cached;

            return true;
        }

        value = null!;

        return false;
    }

    private static string FreshCacheKeyFor(string packageId) => $"nuget:fresh:{packageId.ToLowerInvariant()}";

    private static string LastGoodCacheKeyFor(string packageId) => $"nuget:last-good:{packageId.ToLowerInvariant()}";
}
