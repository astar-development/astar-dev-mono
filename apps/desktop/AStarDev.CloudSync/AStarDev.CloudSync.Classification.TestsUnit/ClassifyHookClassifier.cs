using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ClassifyHookClassifier : IFileClassifier
{
    private static readonly ClassificationTerm Term = ClassificationTestData.Term("holiday", ClassificationTestData.Place);

    private readonly List<string> paths = [];

    internal Func<Exception?> Throws { get; init; } = () => null;

    internal IReadOnlyList<string> Paths
    {
        get
        {
            lock (paths)
                return [.. paths];
        }
    }

    internal Action OnClassify { get; init; } = () => { };

    internal static TermId TermId => Term.Id;

    public Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken)
    {
        lock (paths)
            paths.Add(path.Value ?? string.Empty);

        OnClassify();

        var exception = Throws();
        if (exception is not null)
            throw exception;

        ClassificationCandidate[] candidates = [ClassificationCandidateFactory.Create(Term, ClassificationCategoryFactory.Create(Term.CategoryId, "Place"), SignalKindFactory.CreateFolderName(), ClassificationConfidenceFactory.CreateHigh())];

        return Task.FromResult(FileClassificationResultFactory.Create(path, candidates));
    }
}
