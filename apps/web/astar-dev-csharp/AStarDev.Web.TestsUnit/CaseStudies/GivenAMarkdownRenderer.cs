using AStarDev.Web.CaseStudies;

namespace AStarDev.Web.TestsUnit.CaseStudies;

public class GivenAMarkdownRenderer
{
    private readonly MarkdownRenderer sut = new();

    [Fact]
    public void when_rendering_a_heading_then_it_becomes_an_h2_element()
    {
        sut.RenderToHtml("## The Problem").ShouldContain("<h2");
    }

    [Fact]
    public void when_rendering_a_paragraph_then_it_becomes_a_p_element()
    {
        sut.RenderToHtml("Some body text.").ShouldContain("<p>Some body text.</p>");
    }
}
