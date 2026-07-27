namespace AStarDev.Web.Packages;

/// <summary>A named grouping of NuGet packages shown on the Packages page.</summary>
public sealed record PackageCategory(string Name, string Description, IReadOnlyList<string> PackageIds);
