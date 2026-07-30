namespace Fab4Kids.Web.Catalogue;

/// <summary>Factory for <see cref="SubjectAccent"/>.</summary>
public static class SubjectAccentFactory
{
    public static SubjectAccent Create(string name, string letter, string color, string description, string href) => new(name, letter, color, description, href);
}
