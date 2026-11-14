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

    public Option<TaskCompletionSource> Block { get; set; } = Option.None<TaskCompletionSource>();

    public Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            calls.Add(accountId);

        return Throws.Match(exception => throw exception, () => Block.Match(block => WaitAsync(block, cancellationToken), () => Task.FromResult(Outcome)));
    }

    private async Task<Result<StorageUsage, ProviderError>> WaitAsync(TaskCompletionSource block, CancellationToken cancellationToken)
    {
        await block.Task.WaitAsync(cancellationToken);

        return Outcome;
    }
}
