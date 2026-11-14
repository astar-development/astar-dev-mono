using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountPauseReader : IAccountPauseReader
{
    public Result<IReadOnlyList<AccountId>, ErrorResponse> Paused { get; set; } = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> GetPausedAsync(CancellationToken cancellationToken) => Task.FromResult(Paused);
}
