using System.Text.RegularExpressions;
using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Packages;
using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Pages;

public partial class Packages : ComponentBase
{
    private static readonly Regex NonAlphanumericRuns = new("[^a-z0-9]+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private IReadOnlyList<(string Slug, PackageCategory Category, IReadOnlyList<PackageData> Packages)> categories = [];

    private int totalCount;

    protected override async Task OnInitializedAsync()
    {
        var built = new List<(string Slug, PackageCategory Category, IReadOnlyList<PackageData> Packages)>();

        foreach (var category in PackageCatalog.Categories)
        {
            var results = await Task.WhenAll(category.PackageIds.Select(id => NugetPackageService.GetPackageDataAsync(id, CancellationToken.None)));
            var packages = results
                .Select((result, index) => result.Match(ok => ok, err => PackageDataFactory.Create(category.PackageIds[index], "-", err, 0, "")))
                .ToList();

            built.Add((SlugFor(category.Name), category, packages));
        }

        categories = built;
        totalCount = categories.Sum(c => c.Packages.Count);
    }

    private static string SlugFor(string name) => NonAlphanumericRuns.Replace(name.ToLowerInvariant(), "-");
}
