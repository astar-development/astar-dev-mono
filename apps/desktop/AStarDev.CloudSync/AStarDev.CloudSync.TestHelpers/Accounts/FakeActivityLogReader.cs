using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeActivityLogReader : IActivityLogReader
{
    public Result<IReadOnlyList<ActivityEntry>, ErrorResponse> Entries { get; set; } = Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>([]);

    public List<(Option<AccountId> AccountId, int Count)> Requests { get; } = [];

    public Task<Result<IReadOnlyList<ActivityEntry>, ErrorResponse>> GetRecentAsync(Option<AccountId> accountId, int count, CancellationToken cancellationToken)
    {
        Requests.Add((accountId, count));

        return Task.FromResult(Entries);
    }
}
