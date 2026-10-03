using System.IO.Abstractions;
using System.Text.Json;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

public sealed class ScrapeConfigurationFileReader(IFileSystem fileSystem) : IScrapeConfigurationFileReader
{
    private const string ApplicationSettingsSection = "scrapeConfiguration";
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = fileSystem.File.OpenRead(filePath);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("The configuration file must contain a JSON object.");

        var applicationSettings = Find(root, ApplicationSettingsSection);
        if (applicationSettings is Option<JsonElement>.Some settings)
        {
            RequireApplicationSettings(settings.Value);

            return (root.Deserialize<ScrapeSettingsImportDocument>(jsonOptions) ?? throw new JsonException("The configuration file is empty.")).ToImportDocument();
        }

        RequireAll(root, string.Empty, "userConfiguration", "searchConfiguration", "scrapeDirectories", "baseUrl", "loginUrl");

        return root.Deserialize<ScrapeConfigurationImportDocument>(jsonOptions) ?? throw new JsonException("The configuration file is empty.");
    }

    private static void RequireApplicationSettings(JsonElement settings)
    {
        var prefix = $"{ApplicationSettingsSection}.";
        List<string> missing = [.. MissingFrom(settings, prefix, "userConfiguration", "searchConfiguration", "scrapeDirectories")];
        if (Find(settings, "searchConfiguration") is Option<JsonElement>.Some search) missing.AddRange(MissingFrom(search.Value, $"{prefix}searchConfiguration.", "baseUrl", "loginUrl"));

        ThrowIfAny(missing);
    }

    private static void RequireAll(JsonElement parent, string prefix, params string[] names) => ThrowIfAny([.. MissingFrom(parent, prefix, names)]);

    private static IEnumerable<string> MissingFrom(JsonElement parent, string prefix, params string[] names)
        => names.Where(name => Find(parent, name) is Option<JsonElement>.None).Select(name => $"{prefix}{name}");

    private static void ThrowIfAny(List<string> missing)
    {
        if (missing.Count > 0) throw new JsonException($"The configuration file is missing required values: {string.Join(", ", missing)}.");
    }

    /// <summary>Finds a property by name, ignoring case like the deserializer does; a property that is absent or JSON null is none.</summary>
    private static Option<JsonElement> Find(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object) return Option.None<JsonElement>();

        foreach (var property in parent.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind != JsonValueKind.Null) return Option.Some(property.Value);
        }

        return Option.None<JsonElement>();
    }
}
