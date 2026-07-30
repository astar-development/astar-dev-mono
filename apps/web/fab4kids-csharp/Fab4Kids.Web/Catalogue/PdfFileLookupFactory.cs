namespace Fab4Kids.Web.Catalogue;

/// <summary>Factory for <see cref="PdfFileLookup"/>.</summary>
public static class PdfFileLookupFactory
{
    public static PdfFileLookup Create(PdfCategory category, PdfSubcategory subcategory, PdfFile file) => new(category, subcategory, file);
}
