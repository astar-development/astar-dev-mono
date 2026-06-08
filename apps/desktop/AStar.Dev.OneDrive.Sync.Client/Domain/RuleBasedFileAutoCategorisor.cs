using System.Text.RegularExpressions;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <inheritdoc />
public sealed partial class RuleBasedFileAutoCategorisor : IFileAutoCategorisor
{
    [GeneratedRegex(@"[^a-zA-Z]+")]
    private static partial Regex NonAlphaPattern();

    /// <inheritdoc />
    public FileClassification Categorise(string remotePath)
    {
        string strippedPath = PathNormaliser.StripRootPath(remotePath);
        IReadOnlyList<string> folderSegments = PathNormaliser.GetFolderSegments(strippedPath);
        string filenameStem = PathNormaliser.GetFilenameStem(strippedPath);
        List<string> tokens = Tokenise(filenameStem);

        string level1 = DeriveLevel1(folderSegments, filenameStem, tokens);
        (Option<string> level2, Option<string> level3) = DeriveLevel2AndLevel3(filenameStem, tokens);

        return FileClassificationFactory.Create(level1, level2, level3, isSpecial: false);
    }

    private static string DeriveLevel1(IReadOnlyList<string> folderSegments, string filenameStem, IReadOnlyList<string> tokens)
    {
        foreach (string segment in folderSegments)
        {
            if (Level1Deriver.FolderTypeMap.TryGetValue(segment, out string? mapped) && mapped != "Unclassified")
                return mapped;
        }

        return TokenAnalyser.ExtractPersonName(filenameStem)
            .Match<string>(
                _ => "Person",
                () => tokens.Any(t => TokenAnalyser.ColourWords.Contains(t)) ? "Color" : "Unclassified"
            );
    }

    private static (Option<string> Level2, Option<string> Level3) DeriveLevel2AndLevel3(string filenameStem, IReadOnlyList<string> tokens) =>
        TokenAnalyser.ExtractPersonName(filenameStem)
            .Match<(Option<string>, Option<string>)>(
                name => (Option.Some(name), Option.None<string>()),
                () => DeriveColourLevels(tokens)
            );

    private static (Option<string> Level2, Option<string> Level3) DeriveColourLevels(IReadOnlyList<string> tokens) =>
        TokenAnalyser.ExtractColourPhrase(tokens)
            .Match<(Option<string>, Option<string>)>(
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
