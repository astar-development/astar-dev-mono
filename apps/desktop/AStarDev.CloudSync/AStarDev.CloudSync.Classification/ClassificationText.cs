using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Shared text rules for category names, terms and seed entries.</summary>
internal static class ClassificationText
{
    internal static string Collapse(string? text) => string.Join(' ', UnicodeText.Compose(text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

