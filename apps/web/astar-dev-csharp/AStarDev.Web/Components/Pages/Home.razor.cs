using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Packages;
using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Pages;

public partial class Home : ComponentBase
{
    private static readonly (string Title, string Description, string IconSvg)[] Services =
    [
        ("Fullstack Development",
         "End-to-end feature delivery across .NET backends and modern frontends, with tests from the start.",
         """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/><path d="M7 8l3 3-3 3M13 14h4"/></svg>"""),
        ("Architecture Design",
         "Clear boundaries, explicit contracts, and decision records that outlast the engagement.",
         """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>"""),
        ("Backend Development",
         "Performant, observable .NET services — APIs, workers, and integrations built to production standard.",
         """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="4" rx="1"/><rect x="2" y="10" width="20" height="4" rx="1"/><rect x="2" y="17" width="20" height="4" rx="1"/><circle cx="6" cy="5" r="0.5" fill="currentColor"/><circle cx="6" cy="12" r="0.5" fill="currentColor"/><circle cx="6" cy="19" r="0.5" fill="currentColor"/></svg>"""),
        ("Code Reviews",
         "Structured feedback that improves the code and develops the team. Not a bottleneck — a signal.",
         """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/><path d="M8 11h6M11 8v6"/></svg>"""),
        ("Mentoring",
         "One-to-one and team mentoring grounded in real patterns, not theory.",
         """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.58-7 8-7s8 3 8 7"/><path d="M16 14l2 2 4-4"/></svg>"""),
    ];

    private IReadOnlyList<PackageData> featuredPackages = [];

    protected override async Task OnInitializedAsync()
    {
        var results = await Task.WhenAll(PackageCatalog.Featured.Select(id => NugetPackageService.GetPackageDataAsync(id, CancellationToken.None)));

        featuredPackages = results
            .Select((result, index) => result.Match(ok => ok, err => PackageDataFactory.Create(PackageCatalog.Featured[index], "-", err, 0, "")))
            .ToList();
    }
}
