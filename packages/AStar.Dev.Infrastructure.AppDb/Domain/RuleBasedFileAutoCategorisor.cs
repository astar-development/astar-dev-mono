using System.Text.RegularExpressions;
using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <inheritdoc />
public sealed partial class RuleBasedFileAutoCategorisor : IFileAutoCategorisor
{
    [GeneratedRegex(@"[^a-zA-Z]+")]
    private static partial Regex NonAlphaPattern();

    /// <inheritdoc />
    public Option<FileClassification> Categorise(string remotePath)
    {
        string strippedPath = PathNormaliser.StripRootPath(remotePath);
        var folderSegments = PathNormaliser.GetFolderSegments(strippedPath);
        string filenameStem = PathNormaliser.GetFilenameStem(strippedPath);
        var tokens = Tokenise(filenameStem);

        string level1 = DeriveLevel1(folderSegments, filenameStem, tokens);

        if (level1 == "Unclassified")
            return Option.None<FileClassification>();

        (var level2, var level3) = DeriveLevel2AndLevel3(filenameStem, tokens);

        return Option.Some(FileClassificationFactory.Create(level1, level2, level3, false, false));
    }

    private static string DeriveLevel1(IReadOnlyList<string> folderSegments, string filenameStem, IReadOnlyList<string> tokens)
    {
        foreach (string segment in folderSegments)
        {
            if (Level1Deriver.FolderTypeMap.TryGetValue(segment, out string? mapped) && mapped != "Unclassified")
                return mapped;
        }

        return TokenAnalyser.ExtractPersonName(filenameStem)
            .Match(
                _ => "Person",
                () => tokens.Any(t => TokenAnalyser.ColourWords.Contains(t)) ? "Color" : "Unclassified"
            );
    }

    private static (Option<string> Level2, Option<string> Level3) DeriveLevel2AndLevel3(string filenameStem, IReadOnlyList<string> tokens) =>
        TokenAnalyser.ExtractPersonName(filenameStem)
            .Match(
                name => (Option.Some(name), Option.None<string>()),
                () => DeriveColourLevels(tokens)
            );

    private static (Option<string> Level2, Option<string> Level3) DeriveColourLevels(IReadOnlyList<string> tokens) =>
        TokenAnalyser.ExtractColourPhrase(tokens)
            .Match(
                phrase => BuildColourLevels(phrase),
                () => (Option.None<string>(), Option.None<string>())
            );

    private static (Option<string> Level2, Option<string> Level3) BuildColourLevels(string phrase)
    {
        int spaceIndex = phrase.IndexOf(' ');

        if (spaceIndex < 0)
            return (Option.Some(TitleCase(phrase)), Option.None<string>());

        return (Option.Some(TitleCase(phrase[..spaceIndex])), Option.Some(TitleCase(phrase)));
    }

    private static List<string> Tokenise(string filenameStem) =>
        [.. NonAlphaPattern()
            .Split(filenameStem.ToLowerInvariant())
            .Where(t => t.Length > 0 && !TokenAnalyser.StopWords.Contains(t))];

    private static string TitleCase(string phrase) =>
        string.Join(' ', phrase.Split(' ')
            .Where(w => w.Length > 0)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
}
