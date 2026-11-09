using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.SyncPairs;

internal sealed class NoSelectionRuleWriter : ISelectionRuleWriter
{
    private NoSelectionRuleWriter()
    {
    }

    public static NoSelectionRuleWriter Instance { get; } = new();

    public Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken) => Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
}
