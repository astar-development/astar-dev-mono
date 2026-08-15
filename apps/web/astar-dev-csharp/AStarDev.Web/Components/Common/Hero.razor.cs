using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class Hero : ComponentBase
{
    private const string TerminalPackage = "AStarDev.Utilities";

    private bool AvailableForContracts => Configuration.GetValue("AvailableForContracts", defaultValue: true);
}
