using System.Reflection;
using System.Text.Json;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public static class EmbeddedSeedFiles
{
    private static readonly string[] VocabularyFiles = ["colour", "clothing", "object", "car", "place"];

    public static IReadOnlyList<SeedFileDto> ReadAll() => typeof(ClassificationSeed).Assembly
        .GetManifestResourceNames()
        .Where(name => VocabularyFiles.Any(file => name.EndsWith(".Seed." + file + ".json", StringComparison.Ordinal)))
        .Order(StringComparer.Ordinal)
        .Select(name => Read(typeof(ClassificationSeed).Assembly, name))
        .ToList();

    private static SeedFileDto Read(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var categories = root.GetProperty("categories").EnumerateArray().Select(category => category.GetProperty("name").GetString() ?? string.Empty).ToList();
        var terms = root.GetProperty("terms").EnumerateArray().Select(term => new SeedTermDto(term.GetProperty("text").GetString() ?? string.Empty, term.GetProperty("category").GetString() ?? string.Empty)).ToList();

        return new SeedFileDto(resourceName, root.GetProperty("version").GetString() ?? string.Empty, categories, terms);
    }
}
