using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace AStarDev.Localization;

/// <summary>
/// Loads localised strings from embedded JSON resources named
/// <c>Assets/Localization/{culture}.json</c> (e.g. <c>en-GB.json</c>) in the supplied resource assembly.
///
/// Resource file format:
/// <code>
/// {
///   "locale": "en-GB",
///   "App.Title": "OneDrive Sync",
///   ...
/// }
/// </code>
///
/// Adding a new language:
///   1. Copy <c>en-GB.json</c> to <c>fr-FR.json</c> and translate values.
///   2. Mark the new file as EmbeddedResource in the .csproj.
///   3. The service discovers it automatically at startup.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly CultureInfo fallbackCulture = new("en-GB");

    private readonly Assembly assembly;
    private readonly string resourcePrefix;

    private Dictionary<string, string> strings = [];

    /// <inheritdoc />
    public CultureInfo CurrentCulture { get; private set; } = fallbackCulture;

    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> AvailableCultures { get; private set; }

    /// <inheritdoc />
    public event EventHandler<CultureInfo>? CultureChanged;

    /// <summary>Creates a service that reads the localisation JSON files embedded in <paramref name="resourceAssembly"/>.</summary>
    /// <param name="resourceAssembly">The assembly holding the <c>Assets/Localization/{culture}.json</c> embedded resources.</param>
    public LocalizationService(Assembly resourceAssembly)
    {
        ArgumentNullException.ThrowIfNull(resourceAssembly);

        assembly = resourceAssembly;
        resourcePrefix = assembly.GetName().Name + ".Assets.Localization.";

        AvailableCultures = DiscoverCultures();
    }

    /// <inheritdoc />
    public void Initialise(CultureInfo? requested = null)
    {
        var target = requested ?? fallbackCulture;
        Load(target);
    }

    /// <inheritdoc />
    public string GetLocal(string key) => strings.TryGetValue(key, out string? value) ? value : key;

    /// <inheritdoc />
    public string GetLocal(string key, params object[] args)
    {
        string template = GetLocal(key);
        try
        {
            return string.Format(CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <inheritdoc />
    public async Task SetCultureAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (culture.Name == CurrentCulture.Name)
            return;

        Load(culture);
        CultureChanged?.Invoke(this, CurrentCulture);
    }

    private void Load(CultureInfo target)
    {
        var candidates = new[]
        {
            target.Name,
            target.TwoLetterISOLanguageName,
            fallbackCulture.Name
        }.Distinct();

        foreach (string name in candidates)
        {
            string resourceName = $"{resourcePrefix}{name}.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                continue;

            var loaded = Parse(stream);
            if (loaded.Count == 0)
                continue;

            strings = loaded;

            CurrentCulture = name == target.Name
                ? target
                : SetFallbackIfRequired(name);

            return;
        }

        strings = [];
        CurrentCulture = fallbackCulture;
    }

    private static CultureInfo SetFallbackIfRequired(string name) => name == fallbackCulture.Name ? fallbackCulture : new CultureInfo(name);

    private static Dictionary<string, string> Parse(Stream stream)
    {
        try
        {
            using var doc = JsonDocument.Parse(stream);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name is "locale" or "culture")
                    continue;

                if (prop.Value.ValueKind == JsonValueKind.String)
                    result[prop.Name] = prop.Value.GetString()!;
            }

            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private List<CultureInfo> DiscoverCultures()
    {
        string prefix = resourcePrefix;
        var cultures = new List<CultureInfo>();

        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            string cultureName = name[prefix.Length..^".json".Length];
            try
            {
                cultures.Add(new CultureInfo(cultureName));
            }
            catch (CultureNotFoundException)
            {
                // Skip resource files that aren't valid culture identifiers
            }
        }

        return cultures.Count > 0 ? cultures : [fallbackCulture];
    }
}
