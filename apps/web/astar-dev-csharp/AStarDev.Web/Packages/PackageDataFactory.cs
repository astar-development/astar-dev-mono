namespace AStarDev.Web.Packages;

/// <summary>Factory for <see cref="PackageData"/>.</summary>
public static class PackageDataFactory
{
    public static PackageData Create(string id, string version, string description, long totalDownloads, string projectUrl)
        => new(id, version, description, totalDownloads, projectUrl.Length == 0 ? $"https://www.nuget.org/packages/{id}" : projectUrl);
}
