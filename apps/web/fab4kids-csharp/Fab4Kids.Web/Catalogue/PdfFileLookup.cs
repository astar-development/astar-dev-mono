namespace Fab4Kids.Web.Catalogue;

/// <summary>A single <see cref="PdfFile"/> together with the category and subcategory it belongs to, resolved by ID for the resource detail page.</summary>
public sealed record PdfFileLookup(PdfCategory Category, PdfSubcategory Subcategory, PdfFile File);
