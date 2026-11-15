using System.Reflection;
using System.Text.Json;

namespace AStarDev.CloudSync.TestsUnit.Localisation;

public sealed class GivenTheLocalisationKeys
{
    private static readonly string[] DynamicFamilies = ["Failure.", "Status.Failed.", "Settings.TrashRetention.Days", "Direction.", "Provider."];

    private static readonly string[] ReadOutsideCloudSync = ["Update.", "locale"];

    private static readonly string[] CountSuffixes = [".One", ".Many"];

    private static readonly IReadOnlyDictionary<string, string> Texts = Load();

    private static IReadOnlyDictionary<string, string> Load()
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        var name = assembly.GetManifestResourceNames().Single(resource => resource.EndsWith("en-GB.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("The en-GB texts are an embedded resource.");

        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    }

    private static string WithoutCountSuffix(string key) => CountSuffixes.Where(key.EndsWith).Select(suffix => key[..^suffix.Length]).FirstOrDefault() ?? key;

    private static HashSet<string> KeyLiteralsInCode()
    {
        var prefixes = Texts.Keys.Select(key => key.Split('.')[0]).ToHashSet(StringComparer.Ordinal);

        return [.. LocalisationSources.ProductionFiles(".cs").SelectMany(file => LocalisationSources.KeyLiteralsIn(file.Text, prefixes))];
    }

    [Fact]
    public void when_the_code_names_a_text_key_then_the_key_exists_in_en_gb()
    {
        var missing = KeyLiteralsInCode().Where(key => !Texts.ContainsKey(key) && !Texts.ContainsKey($"{key}.One") && !Texts.ContainsKey($"{key}.Many")).Order().ToList();

        missing.ShouldBeEmpty("Every key the code reads must be in en-GB.json, or the user sees a raw key.");
    }

    [Fact]
    public void when_en_gb_holds_a_key_then_the_code_reads_it_or_it_belongs_to_a_key_family_built_at_run_time()
    {
        var read = KeyLiteralsInCode();

        var unused = Texts.Keys.Where(key => !read.Contains(key) && !read.Contains(WithoutCountSuffix(key)) && !DynamicFamilies.Any(key.StartsWith) && !ReadOutsideCloudSync.Any(key.StartsWith)).Order().ToList();

        unused.ShouldBeEmpty("Delete a key nothing reads, or add its family to the run-time families.");
    }

    [Fact]
    public void when_the_axaml_is_read_then_it_holds_no_literal_user_text()
    {
        var literals = LocalisationSources.ProductionFiles(".axaml").SelectMany(file => LocalisationSources.AxamlLiteralsIn(file.Text).Select(text => $"{file.File}: {text}")).ToList();

        literals.ShouldBeEmpty("User text comes from the localisation service through a bound property.");
    }

    [Fact]
    public void when_the_code_is_read_then_no_user_facing_property_returns_literal_text()
    {
        var literals = LocalisationSources.ProductionFiles(".cs").SelectMany(file => LocalisationSources.UserFacingLiteralsIn(file.Text).Select(text => $"{file.File}: {text}")).ToList();

        literals.ShouldBeEmpty("User text comes from the localisation service.");
    }
}
