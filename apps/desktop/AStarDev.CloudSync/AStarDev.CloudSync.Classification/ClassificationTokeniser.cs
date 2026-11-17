using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.Classification;

/// <summary>Splits text into lower-case, singular, de-duplicated word tokens.</summary>
public static partial class ClassificationTokeniser
{
    private const int StackWordLength = 128;

    private static readonly HashSet<string> InvariantWords = new(StringComparer.Ordinal) { "news", "lens", "series", "species", "chess", "gas", "plus", "bus" };

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal) { "a", "an", "and", "the", "of", "in", "on", "at", "to", "for", "with", "by", "from", "or", "is", "my", "our" };

    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> InvariantWordLookup = InvariantWords.GetAlternateLookup<ReadOnlySpan<char>>();

    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> StopWordLookup = StopWords.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Tokenises the text on separators and camelCase boundaries, dropping pure digit runs, stop words and one-letter words. A word keeps one or two digits next to its letters (mp3, 4k). Plural folding reduces a word and its plural to the same match key (movie and movies both give movy), so tokens are match keys, not words to show a user. Never throws.</summary>
    /// <param name="text">The text to split; null is treated as empty.</param>
    /// <returns>The distinct tokens in order of first appearance.</returns>
    public static IReadOnlyList<string> Tokenise(string? text) => Tokenise(text.AsSpan());

    internal static IReadOnlyList<string> Tokenise(ReadOnlySpan<char> text)
    {
        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var seenLookup = seen.GetAlternateLookup<ReadOnlySpan<char>>();

        foreach (var run in LetterRun().EnumerateMatches(text))
        {
            var runText = text.Slice(run.Index, run.Length);
            foreach (var word in CamelBoundary().EnumerateSplits(runText))
            {
                AddToken(tokens, seenLookup, runText[word]);
            }
        }

        return tokens;
    }

    private static void AddToken(List<string> tokens, HashSet<string>.AlternateLookup<ReadOnlySpan<char>> seen, ReadOnlySpan<char> word)
    {
        if (word.Length <= 1)
            return;

        var buffer = word.Length <= StackWordLength ? stackalloc char[StackWordLength] : new char[word.Length];
        var lower = buffer[..word.Length];
        word.ToLowerInvariant(lower);
        if (StopWordLookup.Contains(lower))
            return;

        var key = InvariantWordLookup.Contains(lower) ? lower : lower[..Canonical(lower[..FoldPlural(lower)])];
        if (seen.Contains(key))
            return;

        var token = new string(key);
        seen.Add(token);
        tokens.Add(token);
    }

    private static int FoldPlural(Span<char> word)
    {
        if (word.Length > 4 && word.EndsWith("ies"))
        {
            word[^3] = 'y';

            return word.Length - 2;
        }

        return word switch
        {
            _ when word.Length > 4 && (word.EndsWith("sses") || word.EndsWith("xes") || word.EndsWith("ches") || word.EndsWith("shes")) => word.Length - 2,
            _ when word.Length > 3 && word[^1] == 's' && !word.EndsWith("ss") && !word.EndsWith("us") && !word.EndsWith("is") => word.Length - 1,
            _ => word.Length
        };
    }

    private static int Canonical(Span<char> word)
    {
        if (word.Length > 4 && word.EndsWith("ie"))
        {
            word[^2] = 'y';

            return word.Length - 1;
        }

        return word.Length > 3 && word[^1] == 'e' ? word.Length - 1 : word.Length;
    }

    [GeneratedRegex(@"(?<!\d)\d{1,2}\p{L}+|\p{L}+\d{1,2}(?!\d)|\p{L}+")]
    private static partial Regex LetterRun();

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})")]
    private static partial Regex CamelBoundary();
}
