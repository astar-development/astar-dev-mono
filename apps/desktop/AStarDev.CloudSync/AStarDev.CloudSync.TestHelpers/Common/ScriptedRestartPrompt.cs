using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class ScriptedRestartPrompt : ISyncRestartPrompt
{
    private readonly List<AccountId> asked = [];

    public SyncRestartChoice Choice { get; set; } = SyncRestartChoiceFactory.CreateKeepCurrent();

    public IReadOnlyList<AccountId> Asked => asked;

    public Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        asked.Add(accountId);

        return Task.FromResult(Choice);
    }
}
