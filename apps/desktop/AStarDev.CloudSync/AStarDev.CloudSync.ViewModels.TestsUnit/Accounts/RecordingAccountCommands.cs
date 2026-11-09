using AStarDev.CloudSync.ViewModels.Accounts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public sealed class RecordingAccountCommands : IAccountCommands
{
    public List<string> Calls { get; } = [];

    public Task SignInAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        Calls.Add($"sign-in:{account.Name}");

        return Task.CompletedTask;
    }

    public Task RemoveAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        Calls.Add($"remove:{account.Name}");

        return Task.CompletedTask;
    }

    public Task ChangeSyncFolderAsync(AccountViewModel account, CancellationToken cancellationToken)
    {
        Calls.Add($"sync-folder:{account.Name}");

        return Task.CompletedTask;
    }
}
