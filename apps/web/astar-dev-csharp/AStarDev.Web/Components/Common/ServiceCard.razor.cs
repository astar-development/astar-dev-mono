using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class ServiceCard : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Description { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public MarkupString Icon { get; set; }
}
