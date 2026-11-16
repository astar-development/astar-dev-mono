namespace AStarDev.CloudSync.Classification;

internal static class SignalCandidates
{
    internal static IReadOnlyList<ClassificationCandidate> Create(IEnumerable<ClassificationTerm> terms, ClassificationVocabulary vocabulary, SignalKind signal) =>
        [.. terms.SelectMany(term => vocabulary.Categories.Where(category => category.Id == term.CategoryId).Take(1).Select(category => ClassificationCandidateFactory.Create(term, category, signal, term.Source is TermSource.Suggested ? ClassificationConfidenceFactory.CreateLow() : ClassificationConfidenceFactory.CreateHigh())))];
}
