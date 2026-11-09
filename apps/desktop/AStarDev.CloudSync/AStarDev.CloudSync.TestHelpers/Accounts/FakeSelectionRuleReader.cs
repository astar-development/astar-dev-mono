using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSelectionRuleReader : ISelectionRuleReader
{
    public Result<IReadOnlyList<SelectionRule>, ErrorResponse> Rules { get; set; } = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<SelectionRule>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Rules);
}
