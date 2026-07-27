namespace Fab4Kids.Web.Catalogue;

/// <summary>Factory for <see cref="PdfCategory"/>.</summary>
public static class PdfCategoryFactory
{
    public static PdfCategory Create(int id, string? name, IReadOnlyList<PdfSubcategory>? subcategories)
        => new(id, string.IsNullOrWhiteSpace(name) ? "Untitled subject" : name.Trim(), subcategories ?? []);
}
