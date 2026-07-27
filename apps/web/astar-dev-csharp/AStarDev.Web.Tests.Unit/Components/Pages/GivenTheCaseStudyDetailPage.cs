using AStarDev.Web.CaseStudies;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.Web.Tests.Unit.Components.Pages;

public class GivenTheCaseStudyDetailPage : Bunit.BunitContext
{
    public GivenTheCaseStudyDetailPage() => Services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

    [Fact]
    public void when_the_slug_matches_a_known_case_study_then_its_content_is_rendered()
    {
        var cut = Render<AStarDev.Web.Components.Pages.CaseStudies.Detail>(parameters => parameters
            .Add(p => p.Slug, "distributed-pipeline-rebuild"));

        cut.Find("h1").TextContent.ShouldBe("Distributed Pipeline Rebuild");
        cut.Find(".case-study-content").InnerHtml.ShouldContain("<h2");
    }

    [Fact]
    public void when_the_slug_does_not_match_any_case_study_then_a_not_found_message_is_shown()
    {
        var cut = Render<AStarDev.Web.Components.Pages.CaseStudies.Detail>(parameters => parameters
            .Add(p => p.Slug, "does-not-exist"));

        cut.Find("h1").TextContent.ShouldBe("Case study not found");
    }
}
