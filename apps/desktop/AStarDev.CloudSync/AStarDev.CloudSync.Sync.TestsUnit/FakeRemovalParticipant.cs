using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeRemovalParticipant(List<string> steps, string name) : IAccountRemovalParticipant
{
    public Result<Unit, ErrorResponse> Outcome { get; set; } = Result.Success<Unit, ErrorResponse>(Unit.Instance);

    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        lock (steps)
            steps.Add(name);

        return Task.FromResult(Outcome);
    }
}
