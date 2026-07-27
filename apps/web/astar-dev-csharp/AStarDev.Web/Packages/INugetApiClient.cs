namespace AStarDev.Web.Packages;

/// <summary>Thin wrapper over the NuGet.org search API, isolated for testability.</summary>
public interface INugetApiClient
{
    Task<PackageData?> FetchAsync(string packageId, CancellationToken cancellationToken);
}
