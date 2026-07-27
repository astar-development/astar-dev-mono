namespace AStarDev.Web.Navigation;

/// <summary>The primary site navigation links, shared by the desktop nav and the mobile drawer.</summary>
public static class SiteNavigation
{
    public static IReadOnlyList<NavLink> Links { get; } =
    [
        NavLinkFactory.Create("/", "Home"),
        NavLinkFactory.Create("/packages", "Packages"),
        NavLinkFactory.Create("/blog", "Blog"),
        NavLinkFactory.Create("/case-studies", "Case Studies"),
        NavLinkFactory.Create("/contact", "Contact"),
    ];

    public static bool IsActive(string href, string currentPath)
    {
        if (href == "/")
        {
            return currentPath == "/";
        }

        return currentPath == href || currentPath.StartsWith(href + "/", StringComparison.Ordinal);
    }
}
