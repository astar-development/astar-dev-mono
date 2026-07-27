using AStar.Dev.Functional.Extensions;

namespace AStarDev.Web.Packages;

/// <summary>Resolves NuGet package display data, with caching and graceful degradation on API failure.</summary>
public interface INugetPackageService
{
    Task<Result<PackageData, string>> GetPackageDataAsync(string packageId, CancellationToken cancellationToken);
}
