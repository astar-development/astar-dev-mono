namespace Fab4Kids.Web.Catalogue;

/// <summary>Factory for <see cref="PdfSearchResult"/>.</summary>
public static class PdfSearchResultFactory
{
    public static PdfSearchResult Create(string categoryName, string categorySlug, string subcategoryName, string subcategorySlug, PdfFile file)
        => new(categoryName, categorySlug, subcategoryName, subcategorySlug, file);
}
