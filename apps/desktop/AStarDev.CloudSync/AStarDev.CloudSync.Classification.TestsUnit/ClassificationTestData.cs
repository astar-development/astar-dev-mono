using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal static class ClassificationTestData
{
    internal static readonly CategoryId Colour = CategoryIdFactory.Create(Guid.NewGuid());
    internal static readonly CategoryId Place = CategoryIdFactory.Create(Guid.NewGuid());

    internal static RelativeSyncPath PathOf(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException());

    internal static ClassificationTerm Term(string text, CategoryId category) => Term(text, category, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateActive());

    internal static ClassificationTerm Term(string text, CategoryId category, TermSource source, TermStatus status) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), text, category, source, status).Match(term => term, () => throw new InvalidOperationException());

    internal static ClassificationVocabulary Vocabulary(params ClassificationTerm[] terms) =>
        ClassificationVocabularyFactory.Create([ClassificationCategoryFactory.Create(Colour, "Colour"), ClassificationCategoryFactory.Create(Place, "Place")], terms);

    internal static string[] Texts(IReadOnlyList<ClassificationCandidate> candidates) => [.. candidates.Select(candidate => candidate.Term.Text)];
}
