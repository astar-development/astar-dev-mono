using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountPauseWriter : IAccountPauseWriter
{
    public Result<PauseState, ErrorResponse> Outcome { get; set; } = Result.Success<PauseState, ErrorResponse>(PauseStateFactory.CreatePaused());

    public List<AccountId> Toggled { get; } = [];

    public Task<Result<PauseState, ErrorResponse>> ToggleAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        Toggled.Add(accountId);

        return Task.FromResult(Outcome);
    }
}
