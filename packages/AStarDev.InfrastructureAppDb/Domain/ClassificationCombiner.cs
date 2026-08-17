using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Combines rule-based and analyser-based <see cref="FileClassification"/> results into a single de-duplicated list.</summary>
public static class ClassificationCombiner
{
    /// <summary>Unions ruleResults and analyserResults, de-duplicating by (Level1, Level2, Level3) with rule results taking precedence. Returns a single Unclassified entry when the combined result is empty.</summary>
    public static IReadOnlyList<FileClassification> Combine(IReadOnlyList<FileClassification> ruleResults, IReadOnlyList<FileClassification> analyserResults)
    {
        var seen = new HashSet<(string, Option<string>, Option<string>)>();
        var combined = new List<FileClassification>();

        foreach (var classification in ruleResults)
        {
            var key = (classification.Level1, classification.Level2, classification.Level3);
            if (seen.Add(key))
                combined.Add(classification);
        }

        foreach (var classification in analyserResults)
        {
            var key = (classification.Level1, classification.Level2, classification.Level3);
            if (seen.Add(key))
                combined.Add(classification);
        }

        if (combined.Count == 0)
            return [FileClassificationFactory.CreateUnclassified()];

        return combined;
    }
}
