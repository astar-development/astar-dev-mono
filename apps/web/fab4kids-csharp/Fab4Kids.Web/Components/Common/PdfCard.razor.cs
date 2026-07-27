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

    private async Task AddToBasketAsync()
    {
        await CartState.AddItemAsync(File.Id, File.Name, File.Price);
        added = true;
        StateHasChanged();

        await Task.Delay(AddedFeedbackDuration);
        added = false;
        StateHasChanged();
    }
}
