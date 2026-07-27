using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class Subject : ComponentBase
{
    private PdfCategory? category;
    private bool hasFiles;

    [Parameter]
    public required string subject { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject]
    public required ICatalogueService CatalogueService { get; set; }

    [Inject]
    public required ILogger<Subject> Logger { get; set; }

    protected override void OnParametersSet()
    {
        if (CatalogueService.GetCategoryBySlug(subject).TryGetValue(out var found))
        {
            category = found;
            hasFiles = found.Subcategories.Any(sub => sub.Files.Count > 0);

            return;
        }

        LogMessage.NotFound(Logger, $"/{subject}");
        if (HttpContext is not null) HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
    }
}
