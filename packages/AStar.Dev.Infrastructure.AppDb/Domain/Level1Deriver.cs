using System.Collections.Frozen;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Derives the Level1 classification value from folder segments and filename tokens.</summary>
public static class Level1Deriver
{
    /// <summary>Maps lowercase folder names to their Level1 classification value.</summary>
    public static readonly IReadOnlyDictionary<string, string> FolderTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["people"] = "Person",
        ["portraits"] = "Person",
        ["places"] = "Place",
        ["landscapes"] = "Place",
        ["events"] = "Event",
        ["photos"] = "Unclassified"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Derives the Level1 value by checking folder segments against <see cref="FolderTypeMap"/> first,
    ///     then falling back to person-name detection, colour detection, and finally returning "Unclassified".
    /// </summary>
    /// <param name="folderSegments">The folder path segments, in order from root to leaf.</param>
    /// <param name="filenameTokens">The tokenised filename stem words.</param>
    public static string Derive(IReadOnlyList<string> folderSegments, IReadOnlyList<string> filenameTokens)
    {
        foreach (string segment in folderSegments)
        {
            if (FolderTypeMap.TryGetValue(segment, out string? mapped))
                return mapped;
        }

        string joinedTokens = string.Join(" ", filenameTokens);

        return TokenAnalyser.ExtractPersonName(joinedTokens)
            .Match(
                _ => "Person",
                () => HasColourToken(filenameTokens) ? "Color" : "Unclassified");
    }

    private static bool HasColourToken(IReadOnlyList<string> filenameTokens)
    {
        foreach (string token in filenameTokens)
        {
            if (TokenAnalyser.ColourWords.Contains(token))
                return true;
        }

        return false;
    }
}
