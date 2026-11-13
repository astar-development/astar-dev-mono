using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GatedSelectionRuleWriter(ISelectionRuleWriter inner, WriteGate gate) : ISelectionRuleWriter
{
    public async Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        await gate.PassAsync();

        return await inner.ApplyAsync(accountId, changes, cancellationToken);
    }
}
