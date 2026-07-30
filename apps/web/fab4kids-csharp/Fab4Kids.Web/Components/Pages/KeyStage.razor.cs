using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class KeyStage : ComponentBase
{
    private PdfCategory? category;
    private PdfSubcategory? subcategory;
    private SubjectAccent? accent;

    [Parameter]
    public required string subject { get; set; }

    [Parameter]
    public required string ks { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject]
    public required ICatalogueService CatalogueService { get; set; }

    [Inject]
    public required ILogger<KeyStage> Logger { get; set; }

    protected override void OnParametersSet()
    {
        if (CatalogueService.GetCategoryBySlug(subject).TryGetValue(out var foundCategory)
            && CatalogueService.GetSubcategoryBySlug(subject, ks).TryGetValue(out var foundSubcategory))
        {
            category = foundCategory;
            subcategory = foundSubcategory;
            accent = SubjectAccents.Find(foundCategory.Name).Match<SubjectAccent?>(value => value, () => null);

            return;
        }

        LogMessage.NotFound(Logger, $"/{subject}/{ks}");
        if (HttpContext is not null) HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
    }
}
