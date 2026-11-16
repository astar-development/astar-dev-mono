using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Merges candidates from several signals into one result.</summary>
public static class ClassificationCombiner
{
    /// <summary>Drops ignored terms, de-duplicates by term text and category with High confidence beating Low, and returns the result. No candidates means unclassified. Never throws.</summary>
    /// <param name="path">The classified file path.</param>
    /// <param name="candidates">Candidates from every signal.</param>
    /// <returns>The merged result.</returns>
    public static FileClassificationResult Combine(RelativeSyncPath path, IEnumerable<ClassificationCandidate> candidates) =>
        FileClassificationResultFactory.Create(path, [.. candidates
            .Where(candidate => candidate.Term.Status is not TermStatus.Ignored)
            .GroupBy(candidate => (candidate.Term.Text, candidate.Term.CategoryId))
            .Select(group => group.OrderBy(candidate => candidate.Confidence is ClassificationConfidence.High ? 0 : 1).First())]);
}
