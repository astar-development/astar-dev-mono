using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Common;

public partial class CopyButton : ComponentBase
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    private bool supported;
    private bool copied;
    private string liveRegionText = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        supported = await JsRuntime.InvokeAsync<bool>("astarClipboard.isSupported", []);
        StateHasChanged();
    }

    private async Task CopyToClipboardAsync()
    {
        var success = await JsRuntime.InvokeAsync<bool>("astarClipboard.copy", [Text]);
        if (!success)
        {
            return;
        }

        copied = true;
        liveRegionText = "Copied to clipboard";
        StateHasChanged();
        _ = ResetAfterDelayAsync();
    }

    private async Task ResetAfterDelayAsync()
    {
        await Task.Delay(2000);
        copied = false;
        liveRegionText = string.Empty;
        await InvokeAsync(StateHasChanged);
    }
}
