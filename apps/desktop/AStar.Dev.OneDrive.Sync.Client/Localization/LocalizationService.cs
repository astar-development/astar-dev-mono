using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Localization;

/// <summary>
/// Loads localised strings from embedded JSON resources named
/// <c>Assets/Localization/{culture}.json</c> (e.g. <c>en-GB.json</c>).
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
    private static readonly CultureInfo FallbackCulture = new("en-GB");

    private readonly Assembly _assembly;
    private readonly string   _resourcePrefix;

    private Dictionary<string, string> _strings = [];

    public CultureInfo CurrentCulture { get; private set; } = FallbackCulture;
    public IReadOnlyList<CultureInfo> AvailableCultures { get; private set; } = [];

    public event EventHandler<CultureInfo>? CultureChanged;

    public LocalizationService()
    {
        _assembly = Assembly.GetExecutingAssembly();
        _resourcePrefix = _assembly.GetName().Name + ".Assets.Localization.";

        AvailableCultures = DiscoverCultures();
    }

    /// <summary>
    /// Must be called once at startup (e.g. from App.OnFrameworkInitializationCompleted).
    /// Loads strings for the requested culture (or en-GB as fallback).
    /// </summary>
    public async Task InitialiseAsync(CultureInfo? requested = null)
    {
        var target = requested ?? FallbackCulture;
        await LoadAsync(target);
    }

    public string GetLocal(string key) => _strings.TryGetValue(key, out string? value) ? value : key;

    public string GetLocal(string key, params object[] args)
    {
        string template = GetLocal(key);
        try
        {
            return string.Format(CurrentCulture, template, args);
        }
        catch(FormatException)
        {
            return template;
        }
    }

    public async Task SetCultureAsync(CultureInfo culture)
    {
        if(culture.Name == CurrentCulture.Name)
            return;
        await LoadAsync(culture);
        CultureChanged?.Invoke(this, CurrentCulture);
    }

    private async Task LoadAsync(CultureInfo target)
    {
        var candidates = new[]
        {
            target.Name,
            target.TwoLetterISOLanguageName,
            FallbackCulture.Name
        }.Distinct();

        foreach(string name in candidates)
        {
            string resourceName = $"{_resourcePrefix}{name}.json";
            await using var stream = _assembly.GetManifestResourceStream(resourceName);
            if(stream is null)
                continue;

            var loaded = await ParseAsync(stream);
            if(loaded.Count == 0)
                continue;

            _strings = loaded;

            CurrentCulture = name == target.Name
                ? target
                : (name == FallbackCulture.Name ? FallbackCulture : new CultureInfo(name));
            return;
        }

        _strings = [];
        CurrentCulture = FallbackCulture;
    }

    private static async Task<Dictionary<string, string>> ParseAsync(Stream stream)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(stream);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach(var prop in doc.RootElement.EnumerateObject())
            {
                if(prop.Name is "locale" or "culture")
                    continue;

                if(prop.Value.ValueKind == JsonValueKind.String)
                    result[prop.Name] = prop.Value.GetString()!;
            }

            return result;
        }
        catch(JsonException)
        {
            return [];
        }
    }

    private List<CultureInfo> DiscoverCultures()
    {
        string prefix = _resourcePrefix;
        var cultures = new List<CultureInfo>();

        foreach(string name in _assembly.GetManifestResourceNames())
        {
            if(!name.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if(!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            string cultureName = name[prefix.Length..^".json".Length];
            try
            {
                cultures.Add(new CultureInfo(cultureName));
            }
            catch(CultureNotFoundException)
            {
                // Skip resource files that aren't valid culture identifiers
            }
        }

        return cultures.Count > 0 ? cultures : [FallbackCulture];
    }
}
