using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Layout;

public sealed partial class Header : ComponentBase
{
    private bool navOpen;

    private void ToggleNav() => navOpen = !navOpen;
}
