using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class CaseStudyTeaser : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Summary { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public IReadOnlyList<string> TechStack { get; set; } = [];

    [Parameter]
    public string? Slug { get; set; }
}
