namespace Fab4Kids.Web.Homepage;

/// <summary>Factory for <see cref="HomeSubjectCard"/>.</summary>
public static class HomeSubjectCardFactory
{
    public static HomeSubjectCard Create(string href, string icon, string label, string description) => new(href, icon, label, description);
}
