using AStar.Dev.Functional.Extensions;
using AStar.Dev.Logging.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AStarDev.Web.Packages;

/// <inheritdoc cref="INugetPackageService" />
public sealed class NugetPackageService(INugetApiClient apiClient, IMemoryCache cache, ILogger<NugetPackageService> logger) : INugetPackageService
{
    private static readonly TimeSpan FreshDuration = TimeSpan.FromHours(6);

    public async Task<Result<PackageData, string>> GetPackageDataAsync(string packageId, CancellationToken cancellationToken)
    {
        var freshKey = FreshCacheKeyFor(packageId);
        var lastGoodKey = LastGoodCacheKeyFor(packageId);

        if (cache.TryGetValue(freshKey, out PackageData? fresh) && fresh is not null)
        {
            return new Result<PackageData, string>.Ok(fresh);
        }

        var fetched = await apiClient.FetchAsync(packageId, cancellationToken);
        if (fetched is not null)
        {
            cache.Set(freshKey, fetched, FreshDuration);
            cache.Set(lastGoodKey, fetched);

            return new Result<PackageData, string>.Ok(fetched);
        }

        if (cache.TryGetValue(lastGoodKey, out PackageData? lastGood) && lastGood is not null)
        {
            LogMessage.Warning(logger, nameof(NugetPackageService), $"NuGet API unreachable for '{packageId}' — using last known good data.");

            return new Result<PackageData, string>.Ok(lastGood);
        }

        LogMessage.Error(logger, $"NuGet API unreachable for '{packageId}' and no cached data is available.");

        return new Result<PackageData, string>.Error($"Package data for '{packageId}' is currently unavailable.");
    }

    private static string FreshCacheKeyFor(string packageId) => $"nuget:fresh:{packageId.ToLowerInvariant()}";

    private static string LastGoodCacheKeyFor(string packageId) => $"nuget:last-good:{packageId.ToLowerInvariant()}";
}
