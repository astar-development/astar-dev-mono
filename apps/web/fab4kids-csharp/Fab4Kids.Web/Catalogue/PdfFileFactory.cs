namespace Fab4Kids.Web.Catalogue;

/// <summary>Factory for <see cref="PdfFile"/>.</summary>
public static class PdfFileFactory
{
    public static PdfFile Create(int id, string? name, string? url, decimal price)
        => new(id, string.IsNullOrWhiteSpace(name) ? "Untitled resource" : name.Trim(), string.IsNullOrWhiteSpace(url) ? string.Empty : url.Trim(), price < 0 ? 0m : price);
}
