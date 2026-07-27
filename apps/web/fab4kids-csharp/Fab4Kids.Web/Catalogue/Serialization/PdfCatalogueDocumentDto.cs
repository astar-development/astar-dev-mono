namespace Fab4Kids.Web.Catalogue.Serialization;

internal sealed record PdfCatalogueDocumentDto(IReadOnlyList<PdfCategoryDto>? Categories);
