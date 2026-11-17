using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class CountingTermStore : ITermStore
{
    private int getAllCalls;

    public int GetAllCalls => Volatile.Read(ref getAllCalls);

    public Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure> Terms { get; set; } = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([]);

    public Result<ClassificationTerm, ClassificationFailure> WriteResult { get; set; } = new Ok<ClassificationTerm, ClassificationFailure>(Term("written", Colour));

    public Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure> WriteManyResult { get; set; } = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([]);

    public Task Gate { get; set; } = Task.CompletedTask;

    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CancellationToken LastToken { get; private set; }

    public async Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref getAllCalls);
        LastToken = cancellationToken;
        var snapshot = Terms;
        Entered.TrySetResult();
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        return snapshot;
    }

    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => Task.FromResult(WriteResult);

    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) => Task.FromResult(WriteManyResult);

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) =>
        Task.FromResult<Result<Option<ClassificationTerm>, ClassificationFailure>>(new Ok<Option<ClassificationTerm>, ClassificationFailure>(Option.None<ClassificationTerm>()));

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) => Task.FromResult(WriteResult);
}
