namespace AStarDev.Web.Packages;

/// <summary>The site's published NuGet packages, grouped for the Packages page and home-page teaser.</summary>
public static class PackageCatalog
{
    public static IReadOnlyList<string> Featured { get; } =
    [
        "AStarDev.Utilities",
        "AStar.Dev.Logging.Extensions",
        "AStar.Dev.Functional.Extensions",
    ];

    public static IReadOnlyList<PackageCategory> Categories { get; } =
    [
        PackageCategoryFactory.Create(
            "Core Utilities",
            "Foundational patterns and extension methods for .NET projects",
            ["AStarDev.Utilities", "AStar.Dev.Functional.Extensions", "AStar.Dev.Technical.Debt.Reporting"]),
        PackageCategoryFactory.Create(
            "Infrastructure & Observability",
            "Logging, health checks, and infrastructure cross-cutting concerns",
            ["AStar.Dev.Logging.Extensions", "AStar.Dev.Api.HealthChecks", "AStar.Dev.Infrastructure", "AStar.Dev.Infrastructure.FilesDb"]),
        PackageCategoryFactory.Create(
            "ASP.NET & API Clients",
            "Extensions and SDK clients for ASP.NET Core and HTTP APIs",
            ["AStar.Dev.AspNet.Extensions", "AStar.Dev.Api.Usage.Sdk", "AStar.Dev.Api.Client.Sdk.Shared"]),
        PackageCategoryFactory.Create(
            "Testing Helpers",
            "Test utilities for unit and integration testing in .NET",
            ["AStar.Dev.Test.Helpers.Unit", "AStar.Dev.Test.Helpers.Minimal.Api", "AStar.Dev.Test.DbContext.Helpers"]),
    ];
}
