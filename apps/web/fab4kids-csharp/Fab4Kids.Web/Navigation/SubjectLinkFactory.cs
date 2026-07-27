namespace Fab4Kids.Web.Navigation;

/// <summary>Factory for <see cref="SubjectLink"/>.</summary>
public static class SubjectLinkFactory
{
    public static SubjectLink Create(string href, string label) => new(href, label);
}
