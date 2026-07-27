namespace Fab4Kids.Web.Catalogue;

/// <summary>Factory for <see cref="PdfSubcategory"/>.</summary>
public static class PdfSubcategoryFactory
{
    public static PdfSubcategory Create(int id, string? name, IReadOnlyList<PdfFile>? files)
        => new(id, string.IsNullOrWhiteSpace(name) ? "Untitled key stage" : name.Trim(), files ?? []);
}
