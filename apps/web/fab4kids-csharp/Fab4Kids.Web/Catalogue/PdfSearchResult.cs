namespace Fab4Kids.Web.Catalogue;

/// <summary>A single matched <see cref="PdfFile"/> together with the category/subcategory it belongs to.</summary>
public sealed record PdfSearchResult(string CategoryName, string CategorySlug, string SubcategoryName, string SubcategorySlug, PdfFile File);
