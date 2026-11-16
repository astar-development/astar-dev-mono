using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeClassificationSuggestions : IClassificationSuggestions
{
    private readonly List<(string Word, CategoryId CategoryId)> promoted = [];
    private readonly List<(string Word, CategoryId CategoryId)> ignored = [];

    public FakeClassificationSuggestions(params ClassificationSuggestion[] suggestions) =>
        ScanOutcome = Result.Success<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>(suggestions);

    public Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure> ScanOutcome { get; set; }

    public TaskCompletionSource? ScanGate { get; set; }

    public bool FailPromote { get; set; }

    public bool FailIgnore { get; set; }

    public int ScanCount { get; private set; }

    public IReadOnlyList<(string Word, CategoryId CategoryId)> Promoted => promoted;

    public IReadOnlyList<(string Word, CategoryId CategoryId)> Ignored => ignored;

    public async Task<Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>> GetSuggestionsAsync(CancellationToken cancellationToken)
    {
        ScanCount++;
        if (ScanGate is not null)
            await ScanGate.Task.WaitAsync(cancellationToken);

        return ScanOutcome;
    }

    public Task<Result<ClassificationTerm, ClassificationFailure>> PromoteAsync(string word, CategoryId categoryId, CancellationToken cancellationToken)
    {
        promoted.Add((word, categoryId));

        return Task.FromResult(Outcome(word, categoryId, TermSourceFactory.CreateUser(), FailPromote));
    }

    public Task<Result<ClassificationTerm, ClassificationFailure>> IgnoreAsync(string word, CategoryId categoryId, CancellationToken cancellationToken)
    {
        ignored.Add((word, categoryId));

        return Task.FromResult(Outcome(word, categoryId, TermSourceFactory.CreateUser(), FailIgnore));
    }

    private static Result<ClassificationTerm, ClassificationFailure> Outcome(string word, CategoryId categoryId, TermSource source, bool fail) =>
        fail
            ? Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable())
            : Result.Success<ClassificationTerm, ClassificationFailure>(ClassificationTestData.Term(word, categoryId, source, TermStatusFactory.CreateActive()));
}
