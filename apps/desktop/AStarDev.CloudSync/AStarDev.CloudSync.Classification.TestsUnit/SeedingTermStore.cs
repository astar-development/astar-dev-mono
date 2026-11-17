using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class SeedingTermStore(Action<int>? onBatch = null) : ITermStore
{
    private readonly List<ClassificationTerm> stored = [];
    private readonly List<int> batchSizes = [];

    public int BatchCalls => batchSizes.Count;

    public IReadOnlyList<int> BatchSizes => batchSizes;

    public int FailFromBatch { get; init; } = int.MaxValue;

    public IReadOnlyList<ClassificationTerm> Stored => stored;

    public void Add(ClassificationTerm term) => stored.Add(term);

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([.. stored]));

    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken)
    {
        batchSizes.Add(terms.Count);
        onBatch?.Invoke(BatchCalls);
        cancellationToken.ThrowIfCancellationRequested();
        if (BatchCalls >= FailFromBatch)
            return Task.FromResult(Result.Failure<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        var results = new List<ClassificationTerm>(terms.Count);
        foreach (var term in terms)
        {
            var existing = stored.FirstOrDefault(item => item.CategoryId == term.CategoryId && item.Text == term.Text);
            if (existing is null)
            {
                existing = term;
                stored.Add(term);
            }

            results.Add(existing);
        }

        return Task.FromResult(Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(results));
    }

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Option<ClassificationTerm>, ClassificationFailure>(Option.None<ClassificationTerm>()));

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound()));
}
