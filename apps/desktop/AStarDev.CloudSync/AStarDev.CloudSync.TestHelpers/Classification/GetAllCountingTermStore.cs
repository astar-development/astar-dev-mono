using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class GetAllCountingTermStore : ITermStore
{
    public List<ClassificationTerm> Terms { get; } = [];

    public int GetAllCount { get; private set; }

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken)
    {
        GetAllCount++;

        return Task.FromResult(Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([.. Terms]));
    }

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
