namespace AStarDev.CloudSync.Classification.TestsUnit;

internal static class EquivalenceCorpus
{
    internal const int Cases = 3000;

    private static readonly string[] Fragments =
    [
        "red", "Red", "RED", "car", "Cars", "CARS", "carS", "babies", "Babies", "boxes", "dresses", "glasses", "churches", "dishes", "bus", "buses", "news", "series", "species", "chess",
        "gas", "plus", "lens", "lenses", "movies", "movie", "Movies", "house", "houses", "pie", "pies", "cookie", "cookies", "toe", "axis", "iris", "virus", "class", "classes",
        "the", "and", "of", "a", "I", "my", "OUR", "mp3", "MP3", "4k", "4K", "2024", "123", "7", "Photo2023", "2024beach", "12abc", "123abc", "abc12", "abc123", "x1234", "1a2b3c",
        "HTMLFile", "RedCarPhoto", "iPhone", "McDonald", "IMG", "DSC", "XMLHttpRequest", "ABCDef", "aBC", "Café", "café", "CAFÉ", "café", "Ünïcödé", "straße", "STRASSE",
        "İstanbul", "ıi", "東京", "東京Tower", "Ελληνικά", "ΣΟΦΙΑ", "ΌΣΑ", "Ǆemal", "ǅungla", "ﬁle", "ǈubljana", "São", "Paulo", "naïve", "coöp", "Ωmega", " ", "😀",
        "new", "york", "New", "York", "los", "angeles", "park", "holiday", "Holidays", "china", "family", "trip"
    ];

    private static readonly string[] Separators = ["", " ", "  ", "_", "-", ".", "\t", ",", "(", ")", "'", "&", "+", "1", "99"];

    internal static IEnumerable<string> Texts(int seed, IReadOnlyList<string> extraFragments)
    {
        var random = new Random(seed);

        return Enumerable.Range(0, Cases).Select(_ => Text(random, extraFragments));
    }

    internal static IEnumerable<string> Paths(int seed, IReadOnlyList<string> extraFragments)
    {
        var random = new Random(seed);

        return Enumerable.Range(0, Cases).Select(_ => string.Join('/', Enumerable.Range(0, random.Next(1, 6)).Select(_ => random.Next(12) == 0 ? string.Empty : Text(random, extraFragments))) + FileSuffix(random));
    }

    internal static IReadOnlyList<string> Tokens(Random random, IReadOnlyList<string> vocabularyTokens) =>
        [.. Enumerable.Range(0, random.Next(0, 9)).Select(_ => random.Next(10) switch
        {
            0 => string.Empty,
            1 => "red car",
            2 => "zzz",
            _ => vocabularyTokens[random.Next(vocabularyTokens.Count)]
        })];

    private static string FileSuffix(Random random) => random.Next(6) switch
    {
        0 => string.Empty,
        1 => "/",
        2 => ".",
        3 => ".tar.gz",
        _ => ".jpg"
    };

    private static string Text(Random random, IReadOnlyList<string> extraFragments) =>
        string.Concat(Enumerable.Range(0, random.Next(0, 7)).Select(_ => Pick(random, extraFragments) + Separators[random.Next(Separators.Length)]));

    private static string Pick(Random random, IReadOnlyList<string> extraFragments) =>
        extraFragments.Count > 0 && random.Next(3) == 0 ? extraFragments[random.Next(extraFragments.Count)] : Fragments[random.Next(Fragments.Length)];
}
