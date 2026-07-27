namespace AStarDev.Web.CaseStudies;

/// <summary>An anonymised client case study shown on the Case Studies page.</summary>
public sealed record CaseStudy(string Slug, string Title, string Summary, IReadOnlyList<string> TechStack, string ContentMarkdown);
