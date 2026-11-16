using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.Classification;

internal static partial class SuggestionWords
{
    internal static IReadOnlyList<string> Split(string path)
    {
        var segments = path.Split('/');
        segments[^1] = Path.GetFileNameWithoutExtension(segments[^1]);

        return [.. segments.SelectMany(segment => LetterRun().Matches(segment)).SelectMany(run => CamelBoundary().Split(run.Value)).Select(word => word.ToLowerInvariant()).Where(word => ClassificationTokeniser.Tokenise(word).Count == 1).Distinct(StringComparer.Ordinal)];
    }

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex LetterRun();

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})")]
    private static partial Regex CamelBoundary();
}
