using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Homepage;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class Home : ComponentBase
{
    private const int MaxFeaturedResources = 4;

    private IReadOnlyList<FeaturedResource> featured = [];

    [Inject]
    public required ICatalogueService CatalogueService { get; set; }

    protected override void OnInitialized() => featured = BuildFeaturedResources();

    private List<FeaturedResource> BuildFeaturedResources() =>
        [.. CatalogueService.GetAllCategories()
            .Select(category => (category, entry: category.Subcategories
                .SelectMany(subcategory => subcategory.Files.Select(file => (subcategory, file)))
                .FirstOrDefault()))
            .Where(pair => pair.entry.file is not null)
            .Take(MaxFeaturedResources)
            .Select(pair =>
            {
                string subjectSlug = pair.category.Name.ToSlug();
                string color = SubjectAccents.Find(pair.category.Name).Match(accent => accent.Color, () => "var(--color-primary)");

                return FeaturedResourceFactory.Create(
                    pair.entry.file,
                    pair.category.Name,
                    color,
                    pair.entry.subcategory.Name,
                    ResourceRoutes.DetailHref(subjectSlug, pair.entry.file.Id));
            })];
}
