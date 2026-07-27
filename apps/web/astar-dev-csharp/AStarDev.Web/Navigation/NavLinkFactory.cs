namespace AStarDev.Web.Navigation;

/// <summary>Factory for <see cref="NavLink"/>.</summary>
public static class NavLinkFactory
{
    public static NavLink Create(string href, string label) => new(href, label);
}
