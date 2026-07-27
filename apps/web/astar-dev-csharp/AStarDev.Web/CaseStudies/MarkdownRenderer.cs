using Markdig;

namespace AStarDev.Web.CaseStudies;

/// <inheritdoc cref="IMarkdownRenderer" />
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public string RenderToHtml(string markdown) => Markdown.ToHtml(markdown, Pipeline);
}
