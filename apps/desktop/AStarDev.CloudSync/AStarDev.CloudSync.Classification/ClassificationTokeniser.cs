using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.Classification;

/// <summary>Splits text into lower-case, singular, de-duplicated word tokens.</summary>
public static partial class ClassificationTokeniser
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal) { "a", "an", "and", "the", "of", "in", "on", "at", "to", "for", "with", "by", "from", "or", "is", "my", "our" };

    /// <summary>Tokenises the text on separators and camelCase boundaries, dropping digits, stop words and one-letter words and folding plurals. Never throws.</summary>
    /// <param name="text">The text to split; null is treated as empty.</param>
    /// <returns>The distinct tokens in order of first appearance.</returns>
    public static IReadOnlyList<string> Tokenise(string? text) =>
        [.. LetterRun().Matches(text ?? string.Empty).SelectMany(run => CamelBoundary().Split(run.Value))
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length > 1 && !StopWords.Contains(word))
            .Select(FoldPlural)
            .Distinct(StringComparer.Ordinal)];

    private static string FoldPlural(string word) => word switch
    {
        _ when word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal) => word[..^3] + "y",
        _ when word.Length > 4 && (word.EndsWith("sses", StringComparison.Ordinal) || word.EndsWith("xes", StringComparison.Ordinal) || word.EndsWith("ches", StringComparison.Ordinal) || word.EndsWith("shes", StringComparison.Ordinal)) => word[..^2],
        _ when word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal) && !word.EndsWith("us", StringComparison.Ordinal) && !word.EndsWith("is", StringComparison.Ordinal) => word[..^1],
        _ => word
    };

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex LetterRun();

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})")]
    private static partial Regex CamelBoundary();
}
