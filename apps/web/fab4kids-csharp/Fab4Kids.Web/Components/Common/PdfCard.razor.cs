using System.Globalization;
using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class PdfCard : ComponentBase
{
    private static readonly CultureInfo PriceCulture = new("en-GB");
    private static readonly TimeSpan AddedFeedbackDuration = TimeSpan.FromMilliseconds(1500);

    private bool added;

    [Parameter, EditorRequired]
    public required PdfFile File { get; set; }

    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public string? SubjectName { get; set; }

    [Parameter]
    public string SubjectColor { get; set; } = "var(--color-primary)";

    [Parameter]
    public string Format { get; set; } = "PDF";

    [Parameter]
    public string? KeyStageLabel { get; set; }

    private async Task AddToBasketAsync()
    {
        await CartState.AddItemAsync(File.Id, File.Name, File.Price, File.Url);
        added = true;
        StateHasChanged();

        await Task.Delay(AddedFeedbackDuration);
        added = false;
        StateHasChanged();
    }
}
