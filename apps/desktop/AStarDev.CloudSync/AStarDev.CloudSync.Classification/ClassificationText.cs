using System.Text;

namespace AStarDev.CloudSync.Classification;

/// <summary>Shared text rules for category names, terms and seed entries.</summary>
internal static class ClassificationText
{
    internal static string Collapse(string? text) => string.Join(' ', (text ?? string.Empty).Normalize(NormalizationForm.FormC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

