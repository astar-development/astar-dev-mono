using AStarDev.Web.Packages;

namespace AStarDev.Web.Tests.Unit.Packages;

public class GivenAPackageCatalog
{
    [Fact]
    public void when_reading_the_featured_packages_then_the_three_featured_ids_are_returned()
    {
        PackageCatalog.Featured.ShouldBe(["AStar.Dev.Utilities", "AStar.Dev.Logging.Extensions", "AStar.Dev.Functional.Extensions"]);
    }

    [Fact]
    public void when_reading_the_categories_then_four_categories_are_returned()
    {
        PackageCatalog.Categories.Count.ShouldBe(4);
        PackageCatalog.Categories.Select(c => c.Name).ShouldBe(["Core Utilities", "Infrastructure & Observability", "ASP.NET & API Clients", "Testing Helpers"]);
    }

    [Fact]
    public void when_reading_the_core_utilities_category_then_it_contains_the_expected_packages()
    {
        var category = PackageCatalog.Categories.Single(c => c.Name == "Core Utilities");

        category.PackageIds.ShouldBe(["AStar.Dev.Utilities", "AStar.Dev.Functional.Extensions", "AStar.Dev.Technical.Debt.Reporting"]);
    }
}
