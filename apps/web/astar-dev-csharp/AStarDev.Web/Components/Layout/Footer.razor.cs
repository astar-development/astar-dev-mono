using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Layout;

public partial class Footer : ComponentBase
{
    private string GithubUrl => Configuration["ExternalLinks:GitHub"] ?? "https://github.com";

    private string NugetUrl => Configuration["ExternalLinks:NuGet"] ?? "https://www.nuget.org";

    private async Task ResetCookiePreferencesAsync()
    {
        await ConsentState.ClearPreferenceAsync();
        Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
    }
}
