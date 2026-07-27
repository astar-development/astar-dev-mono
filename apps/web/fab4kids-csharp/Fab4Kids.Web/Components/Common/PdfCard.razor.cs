using System.Globalization;
using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class PdfCard : ComponentBase
{
    private static readonly CultureInfo PriceCulture = new("en-GB");

    [Parameter, EditorRequired]
    public required PdfFile File { get; set; }
}
