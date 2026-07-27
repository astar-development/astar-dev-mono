namespace Fab4Kids.Web.Navigation;

/// <summary>The primary subject navigation links, shared by the header and footer.</summary>
public static class SiteNavigation
{
    public static IReadOnlyList<SubjectLink> SubjectLinks { get; } =
    [
        SubjectLinkFactory.Create("/maths", "Maths"),
        SubjectLinkFactory.Create("/english", "English"),
        SubjectLinkFactory.Create("/science", "Science"),
        SubjectLinkFactory.Create("/history", "History"),
        SubjectLinkFactory.Create("/geography", "Geography"),
    ];
}
