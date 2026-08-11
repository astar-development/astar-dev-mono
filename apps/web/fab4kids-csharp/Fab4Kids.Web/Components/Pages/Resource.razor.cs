using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class Resource : ComponentBase
{
    private const int MaxRelatedResources = 4;
    private const string DefaultFormat = "PDF";

    private static readonly TimeSpan AddedFeedbackDuration = TimeSpan.FromMilliseconds(1500);

    private PdfFileLookup? lookup;
    private SubjectAccent? accent;
    private List<PdfFile> related = [];
    private bool added;

    [Parameter]
    public required string subject { get; set; }

    [Parameter]
    public required int fileId { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject]
    public required ICatalogueService CatalogueService { get; set; }

    [Inject]
    public required ILogger<Resource> Logger { get; set; }

    protected override void OnParametersSet()
    {
        if (CatalogueService.GetFileById(subject, fileId).TryGetValue(out var found))
        {
            lookup = found;
            accent = SubjectAccents.Find(found.Category.Name).Match<SubjectAccent?>(value => value, () => null);
            related = [.. found.Category.Subcategories
                .SelectMany(subcategory => subcategory.Files)
                .Where(file => file.Id != found.File.Id)
                .Take(MaxRelatedResources)];

            return;
        }

        LogMessage.NotFound(Logger, $"/{subject}/resource/{fileId}");
        if (HttpContext is not null) HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private string SubjectColor => accent?.Color ?? "var(--color-primary)";

    private string Description => $"A {lookup!.Subcategory.Name} {lookup.Category.Name} resource, ready to download and use at home or in the classroom.";

    private async Task AddToBasketAsync()
    {
        await CartState.AddItemAsync(lookup!.File.Id, lookup.File.Name, lookup.File.Price, lookup.File.Url);
        added = true;
        StateHasChanged();

        await Task.Delay(AddedFeedbackDuration, HttpContext?.RequestAborted ?? CancellationToken.None);
        added = false;
        StateHasChanged();
    }
}
