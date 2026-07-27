using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class LegalArticle : ComponentBase
{
    [Parameter, EditorRequired]
    public required string Title { get; set; }

    [Parameter, EditorRequired]
    public required string LastUpdated { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
