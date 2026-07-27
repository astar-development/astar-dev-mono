namespace Fab4Kids.Web.Catalogue;

/// <summary>A subject category (e.g. Maths) grouping key-stage subcategories.</summary>
public sealed record PdfCategory(int Id, string Name, IReadOnlyList<PdfSubcategory> Subcategories);
