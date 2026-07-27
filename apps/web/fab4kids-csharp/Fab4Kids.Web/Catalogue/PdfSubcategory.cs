namespace Fab4Kids.Web.Catalogue;

/// <summary>A key-stage subcategory (e.g. KS1) within a subject category.</summary>
public sealed record PdfSubcategory(int Id, string Name, IReadOnlyList<PdfFile> Files);
