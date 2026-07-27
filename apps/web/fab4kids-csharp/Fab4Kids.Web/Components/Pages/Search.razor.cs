using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class Search : ComponentBase
{
    private IReadOnlyList<PdfSearchResult> results = [];

    [SupplyParameterFromQuery(Name = "q")]
    public string? Q { get; set; }

    [Inject]
    public required ICatalogueService CatalogueService { get; set; }

    protected override void OnParametersSet() => results = CatalogueService.Search(Q ?? string.Empty);
}
