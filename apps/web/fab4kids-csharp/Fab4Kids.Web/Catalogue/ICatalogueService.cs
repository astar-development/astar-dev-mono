using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Catalogue;

/// <summary>Read-only access to the PDF resource catalogue, loaded once at startup.</summary>
public interface ICatalogueService
{
    /// <summary>All subject categories, in catalogue order.</summary>
    IReadOnlyList<PdfCategory> GetAllCategories();

    /// <summary>Looks up a subject category by its URL slug (e.g. <c>"maths"</c>).</summary>
    Option<PdfCategory> GetCategoryBySlug(string categorySlug);

    /// <summary>Looks up a key-stage subcategory by its subject and key-stage URL slugs.</summary>
    Option<PdfSubcategory> GetSubcategoryBySlug(string categorySlug, string subcategorySlug);

    /// <summary>Finds every file whose name, subject, or key stage matches <paramref name="query"/>. Returns an empty list for a blank query.</summary>
    IReadOnlyList<PdfSearchResult> Search(string query);
}
