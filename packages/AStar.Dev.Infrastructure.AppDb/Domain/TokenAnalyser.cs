using System.Collections.Frozen;
using System.Text.RegularExpressions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>
///     Provides static utilities for extracting semantic information — person names and colour phrases —
///     from tokenised file-name stems.
/// </summary>
public static partial class TokenAnalyser
{
    /// <summary>
    ///     Common articles, prepositions, and filler words that carry no semantic meaning in a file name.
    /// </summary>
    public static readonly IReadOnlySet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "on", "in", "of", "with", "at", "and", "it", "its",
        "is", "to", "for", "by", "as", "be", "or", "not", "from", "that", "this",
        "but", "are", "was", "were", "do", "does", "did", "s", "t"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Colour words used both to identify colour phrases and to exclude colour tokens from person-name detection.
    /// </summary>
    public static readonly IReadOnlySet<string> ColourWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "red", "blue", "green", "black", "white", "yellow", "pink", "purple",
        "orange", "brown", "grey", "gray", "gold", "silver", "teal", "navy",
        "maroon", "beige", "coral", "cyan", "magenta"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Physical, concrete nouns that may follow a colour to form a compound phrase (e.g. "red car").
    ///     Also excluded from person-name detection.
    /// </summary>
    private static readonly FrozenSet<string> ConcretePairableNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "car", "dress", "road", "floor", "door", "tree", "house", "cat", "dog", "bird",
        "table", "chair", "shirt", "bag", "hat", "coat", "cup", "box", "boat", "bus",
        "van", "bike", "phone", "book", "bed", "wall", "sky", "sea", "lake", "park",
        "shop", "ring", "shoe", "truck", "plane", "train", "horse", "fish", "rose",
        "ball", "lamp", "sofa", "desk", "clock", "flag", "map", "key", "pen", "coin"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Abstract or generic nouns excluded from person-name detection but not eligible to pair with a colour.
    /// </summary>
    private static readonly FrozenSet<string> AbstractNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "file", "name", "persons", "person", "photo", "picture", "image", "thing",
        "part", "view", "day", "time", "way", "year", "place", "world", "life",
        "hand", "room", "birthday", "party", "wedding", "event", "album",
        "misc", "shot", "img", "archive", "scan", "copy", "temp", "draft"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"[^a-zA-Z]+")]
    private static partial Regex NonAlphaPattern();

    /// <summary>
    ///     Attempts to extract a two-word person name from a file-name string.
    ///     Returns <see cref="Option{T}.Some" /> with the title-cased name when found,
    ///     or <see cref="Option{T}.None" /> when no candidate pair is detected.
    /// </summary>
    /// <param name="text">The raw file name (may include extension and path separators).</param>
    public static Option<string> ExtractPersonName(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Option.None<string>();

        string stem = Path.GetFileNameWithoutExtension(text);

        if (stem.Contains(':'))
        {
            string afterColon = stem[(stem.IndexOf(':') + 1)..];

            return ExtractNamePairFromText(afterColon)
                .Match(Option.Some, () => ExtractNamePairFromText(stem));
        }

        return ExtractNamePairFromText(stem);
    }

    /// <summary>
    ///     Attempts to identify a colour phrase from an already-tokenised, stop-word-filtered token list.
    ///     Returns <see cref="Option{T}.Some" /> containing either a bare colour or a colour-noun compound,
    ///     or <see cref="Option{T}.None" /> when no colour token is present.
    /// </summary>
    /// <param name="tokens">Lower-case, stop-word-filtered tokens derived from a file-name stem.</param>
    public static Option<string> ExtractColourPhrase(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
            return Option.None<string>();

        int colourIndex = -1;
        for (int index = 0; index < tokens.Count; index++)
        {
            if (!ColourWords.Contains(tokens[index]))
                continue;

            colourIndex = index;
            break;
        }

        if (colourIndex < 0)
            return Option.None<string>();

        string colourToken = tokens[colourIndex];
        int nextIndex = colourIndex + 1;

        if (nextIndex < tokens.Count && ConcretePairableNouns.Contains(tokens[nextIndex]))
            return Option.Some(colourToken + " " + tokens[nextIndex]);

        return Option.Some(colourToken);
    }

    private static Option<string> ExtractNamePairFromText(string text)
    {
        List<string> words = [.. NonAlphaPattern()
            .Split(text)
            .Where(word => word.Length >= 2
                           && !StopWords.Contains(word)
                           && !ColourWords.Contains(word)
                           && !ConcretePairableNouns.Contains(word)
                           && !AbstractNouns.Contains(word))];

        if (words.Count < 2)
            return Option.None<string>();

        return Option.Some(Capitalise(words[0]) + " " + Capitalise(words[1]));
    }

    private static string Capitalise(string word) => word.ToTitleCase();
}
