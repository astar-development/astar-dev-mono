using AStarDev.Web.Components.Common;
using AStarDev.Web.Packages;
using Bunit;

namespace AStarDev.Web.TestsUnit.Components.Common;

public class GivenAPackageCard : Bunit.BunitContext
{
    public GivenAPackageCard() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void when_rendered_then_the_package_id_and_description_are_shown()
    {
        var package = PackageDataFactory.Create("AStarDev.Utilities", "1.6.8", "Foundational utilities.", 12_345, "https://www.nuget.org/packages/AStarDev.Utilities");

        var cut = Render<PackageCard>(parameters => parameters.Add(p => p.Package, package));

        cut.Find(".package-name").TextContent.ShouldBe("AStarDev.Utilities");
        cut.Find(".description").TextContent.ShouldBe("Foundational utilities.");
    }

    [Fact]
    public void when_rendered_then_the_download_count_is_formatted()
    {
        var package = PackageDataFactory.Create("AStarDev.Utilities", "1.6.8", "Foundational utilities.", 12_345, "https://www.nuget.org/packages/AStarDev.Utilities");

        var cut = Render<PackageCard>(parameters => parameters.Add(p => p.Package, package));

        cut.Find(".data-row").TextContent.ShouldContain("12.3K downloads");
    }

    [Fact]
    public void when_rendered_then_the_install_command_and_nuget_link_are_shown()
    {
        var package = PackageDataFactory.Create("AStarDev.Utilities", "1.6.8", "Foundational utilities.", 0, "https://www.nuget.org/packages/AStarDev.Utilities");

        var cut = Render<PackageCard>(parameters => parameters.Add(p => p.Package, package));

        cut.Find(".install-code").TextContent.ShouldBe("dotnet add package AStarDev.Utilities");
        cut.Find(".nuget-link").GetAttribute("href").ShouldBe("https://www.nuget.org/packages/AStarDev.Utilities");
    }
}
