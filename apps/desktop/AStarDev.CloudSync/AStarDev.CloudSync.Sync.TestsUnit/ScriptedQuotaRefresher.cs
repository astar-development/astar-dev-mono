using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedQuotaRefresher : IQuotaRefresher, IAccountQuotaRefresher
{
    private readonly Lock gate = new();
    private readonly List<AccountId> calls = [];

    public Result<StorageUsage, ProviderError> Outcome { get; set; } = Result.Success<StorageUsage, ProviderError>(StorageUsageFactory.Create(10, 100));

    public Option<Exception> Throws { get; set; } = Option.None<Exception>();

    public IReadOnlyList<AccountId> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            calls.Add(accountId);

        return Throws.Match(exception => throw exception, () => Task.FromResult(Outcome));
    }
}
