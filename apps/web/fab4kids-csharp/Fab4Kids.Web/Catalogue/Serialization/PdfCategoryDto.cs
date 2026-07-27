namespace Fab4Kids.Web.Catalogue.Serialization;

internal sealed record PdfCategoryDto(int Id, string? Name, IReadOnlyList<PdfSubcategoryDto>? Subcategories);
