using AStarDev.Web.Navigation;
using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Layout;

public partial class NavMenu : ComponentBase
{
    private string GithubUrl => Configuration["ExternalLinks:GitHub"] ?? "https://github.com";

    private string NugetUrl => Configuration["ExternalLinks:NuGet"] ?? "https://www.nuget.org";

    private bool IsActive(string href) => SiteNavigation.IsActive(href, new Uri(Navigation.Uri).AbsolutePath);
}
