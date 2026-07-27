using AStar.Dev.FunctionalParadigm;

namespace AStarDev.Web.Packages;

/// <summary>Thin wrapper over the NuGet.org search API, isolated for testability.</summary>
public interface INugetApiClient
{
    Task<Option<PackageData>> FetchAsync(string packageId, CancellationToken cancellationToken);
}
