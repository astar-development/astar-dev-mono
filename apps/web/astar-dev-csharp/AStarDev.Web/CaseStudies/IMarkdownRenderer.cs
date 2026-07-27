namespace AStarDev.Web.CaseStudies;

/// <summary>Renders Markdown case-study content to HTML.</summary>
public interface IMarkdownRenderer
{
    string RenderToHtml(string markdown);
}
