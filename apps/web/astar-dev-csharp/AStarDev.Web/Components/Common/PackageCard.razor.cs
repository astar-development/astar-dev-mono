using AStarDev.Web.Packages;
using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class PackageCard : ComponentBase
{
    [Parameter, EditorRequired]
    public PackageData Package { get; set; } = null!;

    private string InstallCommand => $"dotnet add package {Package.Id}";

    private string NugetUrl => $"https://www.nuget.org/packages/{Package.Id}";
}
