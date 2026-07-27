using AStarDev.Web.CaseStudies;

namespace AStarDev.Web.Tests.Unit.CaseStudies;

public class GivenACaseStudyCatalog
{
    [Fact]
    public void when_all_case_studies_are_read_then_both_entries_are_present()
    {
        CaseStudyCatalog.All.Select(c => c.Slug).ShouldBe(["global-affiliate-system", "distributed-pipeline-rebuild"]);
    }

    [Fact]
    public void when_finding_by_a_known_slug_then_the_matching_case_study_is_returned()
    {
        var caseStudy = CaseStudyCatalog.FindBySlug("distributed-pipeline-rebuild");

        caseStudy.ShouldNotBeNull();
        caseStudy.Title.ShouldBe("Distributed Pipeline Rebuild");
        caseStudy.TechStack.ShouldBe([".NET 7", "Azure Functions", "Cosmos DB", "Application Insights"]);
    }

    [Fact]
    public void when_finding_by_an_unknown_slug_then_null_is_returned()
    {
        CaseStudyCatalog.FindBySlug("does-not-exist").ShouldBeNull();
    }
}
