namespace AStarDev.Web.Packages;

/// <summary>Factory for <see cref="PackageCategory"/>.</summary>
public static class PackageCategoryFactory
{
    public static PackageCategory Create(string name, string description, IReadOnlyList<string> packageIds) => new(name, description, packageIds);
}
