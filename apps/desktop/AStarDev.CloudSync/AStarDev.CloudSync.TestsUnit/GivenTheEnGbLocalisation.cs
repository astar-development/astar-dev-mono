using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.TestsUnit;

public sealed partial class GivenTheEnGbLocalisation
{
    private static readonly IReadOnlyDictionary<string, string> Texts = Load();

    [GeneratedRegex(@"\b(colou?r\w*|center\w*|cancell?ed|cancell?ing|favou?rite\w*|behaviou?r\w*|gray|catalog\w*|honou?r\w*|neighbou?r\w*|defense|offense|traveled|labeled|modeling|\w+iz(?:e|es|ed|ing|ation|ations)|\w+yz(?:e|es|ed|ing))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpellingCandidates();

    [GeneratedRegex(@"\{(\d+)[^{}]*\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholders();

    private static readonly string[] AllowedWords = ["size", "sizes", "resize", "organisation", "recognised"];

    private static readonly string[] UsSpellings = ["color", "colors", "colored", "center", "centered", "canceled", "canceling", "favorite", "favorites", "behavior", "gray", "catalog", "honor", "neighbor", "defense", "offense", "traveled", "labeled", "modeling", "synchronize", "synchronized", "synchronizing", "synchronization", "organize", "organized", "organization", "recognize", "recognized", "analyze", "analyzed", "customize", "customized", "authorize", "authorized", "initialize", "initialized", "normalize", "normalized", "optimize", "optimized", "minimize", "maximize", "summarize", "utilize"];

    private static IReadOnlyDictionary<string, string> Load()
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        var name = assembly.GetManifestResourceNames().Single(resource => resource.EndsWith("en-GB.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("The en-GB texts are an embedded resource.");

        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    }

    private static IEnumerable<string> UsSpellingsIn(string text) =>
        SpellingCandidates().Matches(text).Select(match => match.Value.ToLowerInvariant()).Where(word => !AllowedWords.Contains(word) && UsSpellings.Contains(word));

    private static IEnumerable<int> PlaceholderIndexes(string text) => Placeholders().Matches(text).Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).Distinct().Order();

    [Fact]
    public void when_the_texts_are_loaded_then_there_are_texts()
    {
        Texts.Count.ShouldBeGreaterThan(300);
    }

    [Fact]
    public void when_every_text_is_read_then_none_uses_a_us_spelling()
    {
        var offenders = Texts.Where(text => UsSpellingsIn(text.Value).Any()).Select(text => $"{text.Key}: {string.Join(", ", UsSpellingsIn(text.Value))}").ToList();

        offenders.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Choose a color", "color")]
    [InlineData("The Center is cancelled", "center")]
    [InlineData("Synchronize now", "synchronize")]
    [InlineData("The sync was canceled", "canceled")]
    public void when_a_text_uses_a_us_spelling_then_the_check_finds_it(string text, string expected)
    {
        UsSpellingsIn(text).ShouldContain(expected);
    }

    [Theory]
    [InlineData("Choose a colour")]
    [InlineData("The centre is cancelled")]
    [InlineData("Synchronise now")]
    [InlineData("Maximum size")]
    public void when_a_text_uses_uk_spelling_then_the_check_finds_nothing(string text)
    {
        UsSpellingsIn(text).ShouldBeEmpty();
    }

    [Fact]
    public void when_every_text_is_read_then_none_is_empty_or_padded_with_spaces()
    {
        Texts.Where(text => string.IsNullOrWhiteSpace(text.Value) || text.Value != text.Value.Trim()).Select(text => text.Key).ShouldBeEmpty();
    }

    [Fact]
    public void when_every_text_is_read_then_its_braces_are_all_numbered_placeholders_counted_from_zero_without_gaps()
    {
        var offenders = Texts
            .Where(text => Placeholders().Replace(text.Value, string.Empty).AsSpan().IndexOfAny('{', '}') >= 0 || !PlaceholderIndexes(text.Value).SequenceEqual(Enumerable.Range(0, PlaceholderIndexes(text.Value).Count())))
            .Select(text => text.Key)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_count_has_a_singular_text_then_it_has_a_plural_text_and_the_other_way_round()
    {
        var ones = Texts.Keys.Where(key => key.EndsWith(".One", StringComparison.Ordinal)).Select(key => key[..^4]).ToHashSet(StringComparer.Ordinal);
        var manys = Texts.Keys.Where(key => key.EndsWith(".Many", StringComparison.Ordinal)).Select(key => key[..^5]).ToHashSet(StringComparer.Ordinal);

        ones.Except(manys).ShouldBeEmpty();
        manys.Except(ones).ShouldBeEmpty();
    }
}
