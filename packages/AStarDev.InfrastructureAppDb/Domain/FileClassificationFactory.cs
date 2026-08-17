using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="FileClassification"/>.</summary>
public static class FileClassificationFactory
{
    /// <summary>Creates a <see cref="FileClassification"/> with the specified levels. If level1 is null or empty/whitespace, defaults to "Unclassified".</summary>
    public static FileClassification Create(string level1, Option<string> level2, Option<string> level3, bool isFamous, bool isInternet)
    {
        string normalizedLevel1 = string.IsNullOrWhiteSpace(level1) ? "Unclassified" : level1;

        return new(normalizedLevel1, level2, level3, isFamous, isInternet);
    }

    /// <summary>Creates the "Unclassified" sentinel used when no rules match a file's path.</summary>
    public static FileClassification CreateUnclassified() => new("Unclassified", Option.None<string>(), Option.None<string>(), false, false);
}
