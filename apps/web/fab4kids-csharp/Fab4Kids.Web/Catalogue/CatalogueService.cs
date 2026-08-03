using System.Text.Json;
using AStar.Dev.FunctionalParadigm;
using AStarDev.Utilities;
using Fab4Kids.Web.Catalogue.Serialization;

namespace Fab4Kids.Web.Catalogue;

/// <summary>
/// Loads the PDF resource catalogue from <c>Data/pdfs.json</c> once at construction and serves it from memory,
/// mirroring the previous Astro site's <c>lib/pdfs.ts</c> behaviour.
/// </summary>
public sealed class CatalogueService : ICatalogueService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<PdfCategory> categories;

    public CatalogueService(IHostEnvironment hostEnvironment)
    {
        string catalogueFilePath = hostEnvironment.ContentRootPath.CombinePath("Data", "pdfs.json");
        string json = File.ReadAllText(catalogueFilePath);
        PdfCatalogueDocumentDto document = JsonSerializer.Deserialize<PdfCatalogueDocumentDto>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Catalogue file '{catalogueFilePath}' could not be parsed.");

        categories = (document.Categories ?? [])
            .Select(category => PdfCategoryFactory.Create(
                category.Id,
                category.Name,
                (category.Subcategories ?? [])
                    .Select(subcategory => PdfSubcategoryFactory.Create(
                        subcategory.Id,
                        subcategory.Name,
                        (subcategory.Files ?? [])
                            .Select(file => PdfFileFactory.Create(file.Id, file.Name, file.Url, file.Price))
                            .ToList()))
                    .ToList()))
            .ToList();
    }

    public IReadOnlyList<PdfCategory> GetAllCategories() => categories;

    public Option<PdfCategory> GetCategoryBySlug(string categorySlug) =>
        categories.FirstOrDefault(category => category.Name.ToSlug().CaseInsensitiveEquals(categorySlug)) is { } category
            ? Option.Some(category)
            : Option.None<PdfCategory>();

    public Option<PdfSubcategory> GetSubcategoryBySlug(string categorySlug, string subcategorySlug) =>
        GetCategoryBySlug(categorySlug).Bind(category =>
            category.Subcategories.FirstOrDefault(subcategory => subcategory.Name.ToSlug().CaseInsensitiveEquals(subcategorySlug)) is { } subcategory
                ? Option.Some(subcategory)
                : Option.None<PdfSubcategory>());

    public Option<PdfFileLookup> GetFileById(string categorySlug, int fileId) =>
        GetCategoryBySlug(categorySlug).Bind(category =>
        {
            var match = category.Subcategories
                .SelectMany(subcategory => subcategory.Files.Select(file => (subcategory, file)))
                .FirstOrDefault(entry => entry.file.Id == fileId);

            return match.file is not null
                ? Option.Some(PdfFileLookupFactory.Create(category, match.subcategory, match.file))
                : Option.None<PdfFileLookup>();
        });

    public IReadOnlyList<PdfSearchResult> Search(string query)
    {
        if (query.IsNullOrWhiteSpace()) return [];

        return categories
            .SelectMany(category => category.Subcategories.SelectMany(subcategory => subcategory.Files
                .Where(file => file.Name.CaseInsensitiveContains(query)
                    || category.Name.CaseInsensitiveContains(query)
                    || subcategory.Name.CaseInsensitiveContains(query))
                .Select(file => PdfSearchResultFactory.Create(category.Name, category.Name.ToSlug(), subcategory.Name, subcategory.Name.ToSlug(), file))))
            .ToList();
    }
}
