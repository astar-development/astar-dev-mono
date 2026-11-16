using System.Reflection;
using System.Text.Json;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Loads every embedded seed JSON file under a resource prefix and merges them into one seed. Versions of the files are joined with a plus sign.</summary>
/// <param name="assembly">The assembly holding the embedded seed files.</param>
/// <param name="resourcePrefix">The manifest resource name prefix that selects the seed files.</param>
public sealed class EmbeddedClassificationSeedSource(Assembly assembly, string resourcePrefix) : IClassificationSeedSource
{
    private const string KindProperty = "kind";

    /// <inheritdoc />
    public Task<Result<ClassificationSeed, ClassificationFailure>> LoadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Try.Run(Read).Match(Result.Success<ClassificationSeed, ClassificationFailure>, _ => Result.Failure<ClassificationSeed, ClassificationFailure>(ClassificationFailureFactory.CreateInvalidSeed())));

    private static SeedTermKind ToKind(string? kind) => kind?.ToLowerInvariant() switch
    {
        null or "word" => SeedTermKindFactory.CreateWord(),
        "extension" => SeedTermKindFactory.CreateExtension(),
        "folder" => SeedTermKindFactory.CreateFolder(),
        _ => throw new JsonException("Unknown seed term kind."),
    };

    private static JsonElement[] Items(JsonElement root, string property) => root.TryGetProperty(property, out var items) ? [.. items.EnumerateArray()] : [];

    private static IEnumerable<SeedTerm> ToTerm(JsonElement term) => SeedTermFactory
        .Create(term.GetProperty("text").GetString() ?? string.Empty, term.GetProperty("category").GetString() ?? string.Empty, ToKind(term.TryGetProperty(KindProperty, out var kind) ? kind.GetString() : null))
        .Match(created => (IEnumerable<SeedTerm>)[created], () => []);

    private ClassificationSeed Read()
    {
        var names = assembly.GetManifestResourceNames().Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal).ToList();
        if (names.Count == 0)
            throw new InvalidOperationException("No seed files.");

        var roots = names.Select(Parse).ToList();
        var versions = roots.Select(root => root.TryGetProperty("version", out var version) ? version.GetString() ?? string.Empty : string.Empty).Where(version => version.Length > 0).Distinct();
        var categories = roots.SelectMany(root => Items(root, "categories")).Select(category => SeedCategoryFactory.Create(category.GetProperty("name").GetString() ?? string.Empty));
        var terms = roots.SelectMany(root => Items(root, "terms")).SelectMany(ToTerm);

        return ClassificationSeedFactory.Create(string.Join('+', versions), [.. categories], [.. terms]);
    }

    private JsonElement Parse(string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException("Seed file missing.");
        using var document = JsonDocument.Parse(stream);

        return document.RootElement.Clone();
    }
}
