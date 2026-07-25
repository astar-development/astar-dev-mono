using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Classifies a remote file path against a set of configured rules.</summary>
public static class FileClassifier
{
    private static readonly char[] Separators = ['/', '-', '_', '.', '+', ' '];

    /// <summary>
    /// Tokenises <paramref name="remotePath"/> and matches each <see cref="KeywordMapping"/> whose keyword appears in the tokens.
    /// Returns an empty list when no mappings match — callers such as <see cref="ClassificationCombiner"/> are responsible for the Unclassified sentinel.
    /// </summary>
    /// <param name="remotePath">The remote path to classify.</param>
    /// <param name="mappings">The keyword mappings to match against.</param>
    public static IReadOnlyList<FileClassification> Classify(string remotePath, IReadOnlyList<FileClassificationCategory> mappings)
    {
        var tokens = Tokenise(remotePath);
        var mappingsById = new Dictionary<FileClassificationCategoryId, FileClassificationCategory>();
        foreach (var mapping in mappings)
            _ = mappingsById.TryAdd(mapping.Id, mapping);
        var matches = mappings
            .Where(mapping =>
            {
                string kw = mapping.Name.ToLowerInvariant();
                if (!kw.Contains(' '))
                    return tokens.Contains(kw);
                string[] words = kw.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                return tokens.Contains(kw) ||
                       tokens.Contains(kw.Replace(" ", string.Empty)) ||
                       words.All(tokens.Contains);
            })
            .Select(mapping => BuildWithAncestry(mapping, mappingsById))
            .OfType<Option<FileClassification>.Some>()
            .Select(some => some.Value)
            .ToList();

        return matches.AsReadOnly();
    }

    private static Option<FileClassification> BuildWithAncestry(FileClassificationCategory mapping, Dictionary<FileClassificationCategoryId, FileClassificationCategory> mappingsById)
    {
        var namesByLevel = new Dictionary<int, string>();
        var current = mapping;

        while (true)
        {
            if (!namesByLevel.TryAdd(current.Level, current.Name))
                return Option.None<FileClassification>();

            if (current.ParentId is not Option<FileClassificationCategoryId>.Some { Value: var parentId })
                break;

            if (!mappingsById.TryGetValue(parentId, out current))
                return Option.None<FileClassification>();
        }

        if (!namesByLevel.TryGetValue(1, out string? level1) || namesByLevel.Count != mapping.Level)
            return Option.None<FileClassification>();

        var level2 = namesByLevel.TryGetValue(2, out string? level2Name) ? Option.Some(level2Name) : Option.None<string>();
        var level3 = namesByLevel.TryGetValue(3, out string? level3Name) ? Option.Some(level3Name) : Option.None<string>();

        return Option.Some(FileClassificationFactory.Create(level1, level2, level3, mapping.IsFamous, mapping.IsInternet));
    }

    private static HashSet<string> Tokenise(string remotePath)
        => [.. remotePath.Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(t => t.ToLowerInvariant())];
}
