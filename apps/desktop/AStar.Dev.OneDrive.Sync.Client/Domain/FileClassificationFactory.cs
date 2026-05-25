using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="FileClassification"/>.</summary>
public static class FileClassificationFactory
{
    /// <summary>Creates a <see cref="FileClassification"/> with the specified levels.</summary>
    public static FileClassification Create(string level1, Option<string> level2, Option<string> level3, bool isSpecial)
    {
        ArgumentNullException.ThrowIfNull(level1);

        return new(level1, level2, level3, isSpecial);
    }

    /// <summary>Creates the "Unclassified" sentinel used when no rules match a file's path.</summary>
    public static FileClassification CreateUnclassified() => new("Unclassified", Option.None<string>(), Option.None<string>(), false);
}
