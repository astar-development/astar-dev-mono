using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Conflicts;

public sealed class FakeHeldDeletionDecider : IHeldDeletionDecider
{
    private readonly List<(string Action, AccountId AccountId, HeldDeletionId Id)> calls = [];
    private readonly Lock gate = new();

    public IReadOnlyList<(string Action, AccountId AccountId, HeldDeletionId Id)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public Func<string, HeldDeletionId, CancellationToken, Task<Result<Unit, HeldDeletionRefusal>>> Outcome { get; set; } = (_, _, _) => Task.FromResult(Result.Success<Unit, HeldDeletionRefusal>(Unit.Instance));

    public Task<Result<Unit, HeldDeletionRefusal>> ConfirmAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken) => RunAsync("Confirm", accountId, id, cancellationToken);

    public Task<Result<Unit, HeldDeletionRefusal>> RestoreAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken) => RunAsync("Restore", accountId, id, cancellationToken);

    private Task<Result<Unit, HeldDeletionRefusal>> RunAsync(string action, AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken)
    {
        lock (gate)
            calls.Add((action, accountId, id));

        return Outcome(action, id, cancellationToken);
    }
}
