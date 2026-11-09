using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSelectionRuleWriter : ISelectionRuleWriter
{
    private readonly List<IReadOnlyList<SelectionChange>> applied = [];

    public Result<Unit, ErrorResponse> Outcome { get; set; } = Result.Success<Unit, ErrorResponse>(Unit.Instance);

    public IReadOnlyList<IReadOnlyList<SelectionChange>> Applied => applied;

    public Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        applied.Add(changes);

        return Task.FromResult(Outcome);
    }
}
