using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.TestsUnit.Localisation;

public sealed partial class GivenTheEnGbLocale
{
    private const string LocaleKey = "locale";

    private static readonly string[] UsSpellings = ["color", "center", "canceled", "canceling", "favorite", "behavior", "gray", "organization", "organize", "synchronize", "analyze", "initialize", "customize", "recognize", "authorize", "finalize", "minimize", "maximize", "normalize", "prioritize", "summarize", "utilize", "defense", "offense", "catalog", "program"];

    private static readonly string[] AllowedUsTerms = ["program files"];

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Entries = new(LoadEntries);

    private static readonly Lazy<string> Source = new(ReadSource);

    [GeneratedRegex("\"([A-Z][A-Za-z0-9]*(?:\\.[A-Za-z0-9]+)+)\"")]
    private static partial Regex KeyLiteral();

    [GeneratedRegex("\\$\"([A-Z][A-Za-z0-9]*(?:\\.[A-Za-z0-9]+)*\\.)\\{")]
    private static partial Regex KeyPrefixLiteral();

    [GeneratedRegex("\\{(\\d+)\\}")]
    private static partial Regex Placeholder();

    private static string CloudSyncFolder() =>
        Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "CloudSyncSourceFolder").Value ?? throw new InvalidOperationException("The build records where the CloudSync projects are.");

    private static IReadOnlyDictionary<string, string> LoadEntries()
    {
        var path = Path.Combine(CloudSyncFolder(), "AStarDev.CloudSync", "Assets", "Localization", "en-GB.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement.EnumerateObject().Where(property => property.Name != LocaleKey).ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
    }

    private static string ReadSource()
    {
        var cloudSync = new DirectoryInfo(CloudSyncFolder());
        var package = new DirectoryInfo(Path.Combine(cloudSync.FullName, "..", "..", "..", "packages", "core", "utilities", "AStarDev.VelopackPublishingAvalonia"));
        var roots = cloudSync.GetDirectories().Where(folder => !folder.Name.Contains("Tests", StringComparison.Ordinal)).Append(package);
        var files = roots.SelectMany(root => root.EnumerateFiles("*.*", SearchOption.AllDirectories)).Where(file => (file.Extension is ".cs" or ".axaml") && !file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        return string.Join('\n', files.Select(file => File.ReadAllText(file.FullName)));
    }

    private static IReadOnlySet<string> LiteralsInCode() => KeyLiteral().Matches(Source.Value).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    private static IReadOnlySet<string> PrefixesInCode() => KeyPrefixLiteral().Matches(Source.Value).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    private static IReadOnlySet<string> Placeholders(string text) => Placeholder().Matches(text).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void when_the_locale_is_read_then_it_declares_en_gb()
    {
        var path = Path.Combine(CloudSyncFolder(), "AStarDev.CloudSync", "Assets", "Localization", "en-GB.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        document.RootElement.GetProperty(LocaleKey).GetString().ShouldBe("en-GB");
    }

    [Fact]
    public void when_a_key_is_used_in_code_then_en_gb_defines_it()
    {
        var prefixes = Entries.Value.Keys.Select(key => key.Split('.')[0]).ToHashSet(StringComparer.Ordinal);
        var used = LiteralsInCode().Where(literal => prefixes.Contains(literal.Split('.')[0]));

        var undefined = used.Where(literal => !Entries.Value.ContainsKey(literal) && !Entries.Value.Keys.Any(key => key.StartsWith(literal + ".", StringComparison.Ordinal))).Order(StringComparer.Ordinal).ToList();

        undefined.ShouldBeEmpty("Every localisation key used in code or AXAML needs an entry in en-GB.json.");
    }

    [Fact]
    public void when_a_key_is_defined_then_code_uses_it()
    {
        var literals = LiteralsInCode();
        var prefixes = PrefixesInCode();

        var unused = Entries.Value.Keys.Where(key => !literals.Contains(key) && !literals.Any(literal => key.StartsWith(literal + ".", StringComparison.Ordinal)) && !prefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal))).Order(StringComparer.Ordinal).ToList();

        unused.ShouldBeEmpty("Every key in en-GB.json must be used, or removed.");
    }

    [Fact]
    public void when_a_text_has_placeholders_then_they_are_numbered_from_zero_without_gaps()
    {
        var gaps = Entries.Value.Where(entry => Placeholders(entry.Value).Count > 0 && !Placeholders(entry.Value).Order().Select(int.Parse).SequenceEqual(Enumerable.Range(0, Placeholders(entry.Value).Count))).Select(entry => entry.Key).Order(StringComparer.Ordinal).ToList();

        gaps.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_plural_pair_is_read_then_the_singular_uses_no_placeholder_the_plural_lacks()
    {
        var mismatched = Entries.Value.Keys.Where(key => key.EndsWith(".One", StringComparison.Ordinal)).Select(key => key[..^4]).Where(stem => Entries.Value.TryGetValue(stem + ".Many", out var many) && !Placeholders(Entries.Value[stem + ".One"]).IsSubsetOf(Placeholders(many))).Order(StringComparer.Ordinal).ToList();

        mismatched.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_plural_key_exists_then_its_partner_exists()
    {
        var orphans = Entries.Value.Keys.Where(key => (key.EndsWith(".One", StringComparison.Ordinal) && !Entries.Value.ContainsKey(key[..^4] + ".Many")) || (key.EndsWith(".Many", StringComparison.Ordinal) && !Entries.Value.ContainsKey(key[..^5] + ".One"))).Order(StringComparer.Ordinal).ToList();

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_text_is_read_then_it_has_no_us_spelling()
    {
        var offenders = Entries.Value.Where(entry => !AllowedUsTerms.Any(term => entry.Value.Contains(term, StringComparison.OrdinalIgnoreCase)) && UsSpellings.Any(spelling => entry.Value.Contains(spelling, StringComparison.OrdinalIgnoreCase))).Select(entry => entry.Key).Order(StringComparer.Ordinal).ToList();

        offenders.ShouldBeEmpty("en-GB is the MVP locale, so use UK spellings.");
    }

    [Fact]
    public void when_a_text_is_read_then_it_is_not_empty()
    {
        Entries.Value.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key).ShouldBeEmpty();
    }
}
