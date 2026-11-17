using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class RecordingVocabularyProvider(Result<ClassificationVocabulary, ClassificationFailure> result) : IClassificationVocabularyProvider
{
    private int invalidations;

    public int Invalidations => Volatile.Read(ref invalidations);

    public long Generation => Volatile.Read(ref invalidations);

    public Task<Result<ClassificationVocabulary, ClassificationFailure>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(result);

    public void Invalidate() => Interlocked.Increment(ref invalidations);
}
