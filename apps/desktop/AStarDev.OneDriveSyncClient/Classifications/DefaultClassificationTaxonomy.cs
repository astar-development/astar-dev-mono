using System.Globalization;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>The default Level 1 categories, with their Level 2 children, inserted by <see cref="IDefaultCategorySeedingService"/>.</summary>
internal static class DefaultClassificationTaxonomy
{
    private static readonly IReadOnlyList<string> colourNames = [.. TokenAnalyser.ColourWords.Select(word => string.Create(CultureInfo.InvariantCulture, $"{char.ToUpperInvariant(word[0])}{word[1..]}")).Order(StringComparer.Ordinal)];

    /// <summary>Level 1 categories in insertion order: name, whether they are included in search, and their Level 2 child names.</summary>
    internal static IReadOnlyList<(string Name, bool IncludeInSearch, IReadOnlyList<string> Children)> Roots { get; } =
    [
        ("Unclassified", true, []),
        ("Colour", true, colourNames),
        ("Person", true, []),
        ("Place", true, []),
        ("Event", true, []),
        ("Photo", true, ["Screenshot", "Wallpaper"]),
        ("Video", true, []),
        ("Audio", true, []),
        ("Document", true, ["Invoice", "Manual", "Presentation", "Receipt", "Spreadsheet", "Text"]),
        ("Code", false, ["Script"]),
        ("Archive", false, []),
        ("Other", false, [])
    ];
}
