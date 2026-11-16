using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class SuggestionTermStore(params ClassificationTerm[] initial) : ITermStore
{
    private readonly List<ClassificationTerm> terms = [.. initial];
    private readonly Lock gate = new();

    internal bool Fails { get; set; }

    internal IReadOnlyList<ClassificationTerm> Snapshot()
    {
        lock (gate)
        {
            return [.. terms];
        }
    }

    public async Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        return Fails ? Result.Failure<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()) : Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(Snapshot());
    }

    public async Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken)
    {
        await Task.Yield();

        lock (gate)
        {
            terms.RemoveAll(existing => existing.Id == term.Id);
            terms.Add(term);
        }

        return Result.Success<ClassificationTerm, ClassificationFailure>(term);
    }

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) => throw new NotSupportedException();
}
