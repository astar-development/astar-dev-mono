using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Layout;

public partial class Footer : ComponentBase
{
    private static int CurrentYear => DateTime.UtcNow.Year;
}
