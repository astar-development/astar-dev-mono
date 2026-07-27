namespace Fab4Kids.Web.Catalogue;

/// <summary>A single downloadable PDF resource within a subcategory.</summary>
public sealed record PdfFile(int Id, string Name, string Url, decimal Price);
