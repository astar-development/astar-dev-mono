namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Extension methods for <see cref="FileClassificationKeyword"/>.</summary>
public static class FileClassificationKeywordExtensions
{
    /// <summary>Resolves the effective IsSpecial flag, preferring the keyword's override when present.</summary>
    public static bool ResolveIsSpecial(this FileClassificationKeyword keyword, FileClassification classification) =>
        keyword.IsFamous.Match(overrideValue => overrideValue, () => classification.IsFamous);
}
