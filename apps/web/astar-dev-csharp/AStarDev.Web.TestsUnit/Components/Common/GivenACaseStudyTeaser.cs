using AStarDev.Web.Components.Common;
using Bunit;

namespace AStarDev.Web.TestsUnit.Components.Common;

public class GivenACaseStudyTeaser : Bunit.BunitContext
{
    [Fact]
    public void when_a_slug_is_provided_then_the_teaser_links_to_the_case_study()
    {
        var cut = Render<CaseStudyTeaser>(parameters => parameters
            .Add(p => p.Title, "Distributed Pipeline Rebuild")
            .Add(p => p.Summary, "Summary text.")
            .Add(p => p.TechStack, [".NET 7", "Azure Functions"])
            .Add(p => p.Slug, "distributed-pipeline-rebuild"));

        cut.Find("a.teaser-link").GetAttribute("href").ShouldBe("/case-studies/distributed-pipeline-rebuild");
        cut.Find(".read-link").TextContent.ShouldContain("Read case study");
    }

    [Fact]
    public void when_no_slug_is_provided_then_a_coming_soon_label_is_shown_instead_of_a_link()
    {
        var cut = Render<CaseStudyTeaser>(parameters => parameters
            .Add(p => p.Title, "Global Affiliate System")
            .Add(p => p.Summary, "Summary text.")
            .Add(p => p.TechStack, [".NET 2-5"]));

        cut.FindAll("a.teaser-link").ShouldBeEmpty();
        cut.Find(".coming-soon").TextContent.ShouldBe("Full case study coming soon");
    }
}
