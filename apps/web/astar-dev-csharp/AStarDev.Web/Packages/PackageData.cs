namespace AStarDev.Web.Packages;

/// <summary>A single NuGet package's display data, sourced from the NuGet.org search API.</summary>
public sealed record PackageData(string Id, string Version, string Description, long TotalDownloads, string ProjectUrl);
