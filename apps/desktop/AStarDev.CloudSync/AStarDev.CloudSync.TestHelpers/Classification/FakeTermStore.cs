using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeTermStore : ITermStore
{
    private readonly List<ClassificationTerm> terms;
    private readonly List<(TermId TermId, TermStatus Status)> statusCalls = [];

    public FakeTermStore(params ClassificationTerm[] terms) => this.terms = [.. terms];

    public Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>? GetAllOutcome { get; set; }

    public bool FailSetStatus { get; set; }

    public IReadOnlyList<(TermId TermId, TermStatus Status)> StatusCalls => statusCalls;

    public IReadOnlyList<ClassificationTerm> Stored => terms;

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(GetAllOutcome ?? Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([.. terms]));

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken)
    {
        statusCalls.Add((termId, status));
        var index = terms.FindIndex(term => term.Id == termId);
        if (FailSetStatus || index < 0)
            return Task.FromResult(Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        var current = terms[index];
        terms[index] = ClassificationTermFactory.Create(current.Id, current.Text, current.CategoryId, current.Source, status).Match(updated => updated, () => current);

        return Task.FromResult(Result.Success<ClassificationTerm, ClassificationFailure>(terms[index]));
    }

    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
