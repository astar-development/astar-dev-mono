using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ReclassifyFakeClassifier(ClassificationTerm term) : IFileClassifier
{
    private int classified;

    internal Action<int> AfterClassified { get; init; } = _ => { };

    internal int Classified => classified;

    public Task<FileClassificationResult> ClassifyAsync(RelativeSyncPath path, CancellationToken cancellationToken)
    {
        var candidates = (path.Value ?? string.Empty).Contains("holiday", StringComparison.OrdinalIgnoreCase)
            ? [ClassificationCandidateFactory.Create(term, ClassificationCategoryFactory.Create(term.CategoryId, "Place"), SignalKindFactory.CreateFolderName(), ClassificationConfidenceFactory.CreateHigh())]
            : Array.Empty<ClassificationCandidate>();
        AfterClassified(Interlocked.Increment(ref classified));

        return Task.FromResult(FileClassificationResultFactory.Create(path, candidates));
    }
}
