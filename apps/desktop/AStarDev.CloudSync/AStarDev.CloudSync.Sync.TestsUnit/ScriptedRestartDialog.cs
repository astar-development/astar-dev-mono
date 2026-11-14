using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedRestartDialog : IConfirmSyncRestartDialog
{
    private readonly Lock gate = new();
    private readonly List<AccountId> asked = [];

    public SyncRestartChoice Choice { get; set; } = SyncRestartChoiceFactory.CreateKeepCurrent();

    public Action BeforeAnswering { get; set; } = () => { };

    public IReadOnlyList<AccountId> Asked
    {
        get
        {
            lock (gate)
                return [.. asked];
        }
    }

    public Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        lock (gate)
            asked.Add(accountId);

        BeforeAnswering();

        return Task.FromResult(Choice);
    }
}
