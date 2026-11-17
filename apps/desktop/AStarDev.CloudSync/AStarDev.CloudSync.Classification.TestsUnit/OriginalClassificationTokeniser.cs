using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal static partial class OriginalClassificationTokeniser
{
    private static readonly HashSet<string> InvariantWords = new(StringComparer.Ordinal) { "news", "lens", "series", "species", "chess", "gas", "plus", "bus" };

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal) { "a", "an", "and", "the", "of", "in", "on", "at", "to", "for", "with", "by", "from", "or", "is", "my", "our" };

    internal static IReadOnlyList<string> Tokenise(string? text) =>
        [.. LetterRun().Matches(text ?? string.Empty).SelectMany(run => CamelBoundary().Split(run.Value))
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length > 1 && !StopWords.Contains(word))
            .Select(word => InvariantWords.Contains(word) ? word : Canonical(FoldPlural(word)))
            .Distinct(StringComparer.Ordinal)];

    private static string FoldPlural(string word) => word switch
    {
        _ when word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal) => word[..^3] + "y",
        _ when word.Length > 4 && (word.EndsWith("sses", StringComparison.Ordinal) || word.EndsWith("xes", StringComparison.Ordinal) || word.EndsWith("ches", StringComparison.Ordinal) || word.EndsWith("shes", StringComparison.Ordinal)) => word[..^2],
        _ when word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal) && !word.EndsWith("us", StringComparison.Ordinal) && !word.EndsWith("is", StringComparison.Ordinal) => word[..^1],
        _ => word
    };

    private static string Canonical(string word) => word switch
    {
        _ when word.Length > 4 && word.EndsWith("ie", StringComparison.Ordinal) => word[..^2] + "y",
        _ when word.Length > 3 && word.EndsWith('e') => word[..^1],
        _ => word
    };

    [GeneratedRegex(@"(?<!\d)\d{1,2}\p{L}+|\p{L}+\d{1,2}(?!\d)|\p{L}+")]
    private static partial Regex LetterRun();

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})")]
    private static partial Regex CamelBoundary();
}
