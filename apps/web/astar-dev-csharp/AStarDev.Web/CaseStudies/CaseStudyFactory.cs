namespace AStarDev.Web.CaseStudies;

/// <summary>Factory for <see cref="CaseStudy"/>.</summary>
public static class CaseStudyFactory
{
    public static CaseStudy Create(string slug, string title, string summary, IReadOnlyList<string> techStack, string contentMarkdown)
        => new(slug, title, summary, techStack, contentMarkdown);
}
