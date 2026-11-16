using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class SeedingTermStore(Action<int>? onUpsert = null) : ITermStore
{
    private readonly List<ClassificationTerm> stored = [];

    public int UpsertCalls { get; private set; }

    public int FailFromCall { get; init; } = int.MaxValue;

    public IReadOnlyList<ClassificationTerm> Stored => stored;

    public void Add(ClassificationTerm term) => stored.Add(term);

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([.. stored]));

    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken)
    {
        UpsertCalls++;
        onUpsert?.Invoke(UpsertCalls);
        cancellationToken.ThrowIfCancellationRequested();
        if (UpsertCalls >= FailFromCall)
            return Task.FromResult(Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        var existing = stored.FirstOrDefault(item => item.CategoryId == term.CategoryId && item.Text == term.Text);
        if (existing is null)
        {
            existing = term;
            stored.Add(term);
        }

        return Task.FromResult(Result.Success<ClassificationTerm, ClassificationFailure>(existing));
    }

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Option<ClassificationTerm>, ClassificationFailure>(Option.None<ClassificationTerm>()));

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound()));
}
