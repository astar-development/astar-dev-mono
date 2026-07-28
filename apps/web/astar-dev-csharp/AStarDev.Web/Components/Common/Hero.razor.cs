using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class Hero : ComponentBase
{
    private const string TerminalPackage = "AStar.Dev.Utilities";
    private const string TerminalVersion = "1.6.8";

    private bool AvailableForContracts => Configuration.GetValue("AvailableForContracts", defaultValue: true);
}
