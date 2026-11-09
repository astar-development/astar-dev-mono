using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class RecordingFolderSelectionDialog : IFolderSelectionDialog
{
    private readonly List<(Option<AccountId> AccountId, CancellationToken Token)> shown = [];

    public IReadOnlyList<(Option<AccountId> AccountId, CancellationToken Token)> Shown => shown;

    public Task ShowAsync(Option<AccountId> accountId, CancellationToken cancellationToken)
    {
        shown.Add((accountId, cancellationToken));

        return Task.CompletedTask;
    }
}
