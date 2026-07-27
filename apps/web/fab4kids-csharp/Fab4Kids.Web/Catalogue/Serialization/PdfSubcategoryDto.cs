namespace Fab4Kids.Web.Catalogue.Serialization;

internal sealed record PdfSubcategoryDto(int Id, string? Name, IReadOnlyList<PdfFileDto>? Files);
