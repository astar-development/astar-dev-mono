using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Represents the classification tags derived from a synced file's remote path.</summary>
public sealed record FileClassification(string Level1, Option<string> Level2, Option<string> Level3, bool IsFamous, bool IsInternet)
{
    /// <summary>The most-specific classification level: Level3 if present, then Level2, then Level1.</summary>
    public string TagName => Level3.MapOrDefault(v => v, Level2.MapOrDefault(v => v, Level1));
}
