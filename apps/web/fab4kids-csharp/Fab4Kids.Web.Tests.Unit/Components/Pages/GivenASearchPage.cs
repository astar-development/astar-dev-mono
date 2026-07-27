using Bunit;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Pages;

public class GivenASearchPage : Bunit.BunitContext
{
    private readonly ICatalogueService catalogueService = Substitute.For<ICatalogueService>();

    public GivenASearchPage()
    {
        catalogueService.Search(Arg.Any<string>()).Returns([]);
        Services.AddSingleton(catalogueService);
    }

    private void Navigate(string uri) => Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    [Fact]
    public void when_no_query_has_been_entered_then_no_results_are_shown()
    {
        var cut = Render<Search>();

        cut.FindAll("li").ShouldBeEmpty();
    }

    [Fact]
    public void when_a_query_matches_a_file_then_the_result_is_shown_with_its_breadcrumb()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);
        var result = PdfSearchResultFactory.Create("Maths", "maths", "KS1", "ks1", file);
        catalogueService.Search("times").Returns([result]);
        Navigate("/search?q=times");

        var cut = Render<Search>();

        cut.Find("p.search-count").TextContent.ShouldContain("1 resource found");
        cut.Find("a[href='/maths']").TextContent.ShouldBe("Maths");
        cut.Find("a[href='/maths/ks1']").TextContent.ShouldBe("KS1");
    }

    [Fact]
    public void when_a_query_matches_nothing_then_the_no_results_message_is_shown()
    {
        catalogueService.Search("geography").Returns([]);
        Navigate("/search?q=geography");

        var cut = Render<Search>();

        cut.Find("p.search-empty").TextContent.ShouldContain("geography");
    }
}
