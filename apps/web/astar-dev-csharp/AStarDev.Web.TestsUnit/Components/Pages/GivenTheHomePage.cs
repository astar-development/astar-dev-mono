using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Components.Pages;
using AStarDev.Web.Packages;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Components.Pages;

public class GivenTheHomePage : Bunit.BunitContext
{
    public GivenTheHomePage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var nugetPackageService = Substitute.For<INugetPackageService>();
        nugetPackageService.GetPackageDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<Result<PackageData, string>>(
                PackageDataFactory.Create(callInfo.ArgAt<string>(0), "1.0.0", "A package.", 100, "")));

        Services.AddSingleton(nugetPackageService);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    }

    [Fact]
    public void when_rendered_then_the_hero_heading_is_shown()
    {
        var cut = Render<Home>();

        cut.Find("#hero-heading").TextContent.ShouldBe(".NET Staff Engineer & Architect");
    }

    [Fact]
    public void when_rendered_then_five_service_cards_are_shown()
    {
        var cut = Render<Home>();

        cut.FindAll(".services-grid > article").Count.ShouldBe(5);
    }

    [Fact]
    public void when_rendered_then_the_featured_packages_are_shown()
    {
        var cut = Render<Home>();

        cut.FindAll(".packages-grid > article").Count.ShouldBe(3);
    }

    [Fact]
    public void when_rendered_then_both_case_study_teasers_are_shown()
    {
        var cut = Render<Home>();

        cut.FindAll(".case-studies-list article").Count.ShouldBe(2);
    }
}
