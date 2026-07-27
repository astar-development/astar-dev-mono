using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class StatusBadge : ComponentBase
{
    [Parameter, EditorRequired]
    public StatusBadgeVariant Variant { get; set; }

    private string Label => Variant == StatusBadgeVariant.Available ? "Available for contracts" : "Open source contributor";
}
