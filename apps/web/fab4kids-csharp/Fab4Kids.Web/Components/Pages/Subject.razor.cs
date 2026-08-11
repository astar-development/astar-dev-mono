using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Fab4Kids.Web.Catalogue;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class Subject : ComponentBase
{
    private const int PageSize = 8;
    private static readonly IReadOnlyList<string> FormatFilters = ["PDF", "Word", "Physical"];

    private PdfCategory? category;
    private SubjectAccent? accent;
    private List<SubjectResourceItem> allItems = [];
    private List<string> filterOptions = [];
    private string selectedFilter = "All";
    private int visibleCount = PageSize;

    [Parameter]
    public required string subject { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject]
    public required ICatalogueService CatalogueService { get; set; }

    [Inject]
    public required ILogger<Subject> Logger { get; set; }

    private IEnumerable<SubjectResourceItem> FilteredItems =>
        selectedFilter switch
        {
            "All" => allItems,
            _ when FormatFilters.Contains(selectedFilter) => allItems.Where(item => item.Format == selectedFilter),
            _ => allItems.Where(item => item.KeyStage == selectedFilter),
        };

    private List<SubjectResourceItem> VisibleItems => [.. FilteredItems.Take(visibleCount)];

    private bool HasMoreItems => FilteredItems.Count() > visibleCount;

    protected override void OnParametersSet()
    {
        if (CatalogueService.GetCategoryBySlug(subject).TryGetValue(out var found))
        {
            category = found;
            accent = SubjectAccents.Find(found.Name).Match<SubjectAccent?>(value => value, () => null);
            allItems = [.. found.Subcategories.SelectMany(subcategory => subcategory.Files.Select(file => new SubjectResourceItem(file, subcategory.Name, "PDF")))];
            filterOptions = ["All", .. found.Subcategories.Select(subcategory => subcategory.Name), .. FormatFilters];
            selectedFilter = "All";
            visibleCount = PageSize;

            return;
        }

        LogMessage.NotFound(Logger, $"/{subject}");
        if (HttpContext is not null) HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private void SelectFilter(string filter)
    {
        selectedFilter = filter;
        visibleCount = PageSize;
    }

    private void LoadMore() => visibleCount += PageSize;

    private void ClearFilter() => SelectFilter("All");

    private sealed record SubjectResourceItem(PdfFile File, string KeyStage, string Format);
}
