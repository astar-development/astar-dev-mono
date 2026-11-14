namespace AStarDev.CloudSync.ViewModels.Conflicts;

internal sealed class HeldDeletionActions(Func<HeldDeletionItemViewModel, CancellationToken, Task> confirm, Func<HeldDeletionItemViewModel, CancellationToken, Task> restore, Action<HeldDeletionItemViewModel> decideLater)
{
    internal Func<HeldDeletionItemViewModel, CancellationToken, Task> Confirm { get; } = confirm;

    internal Func<HeldDeletionItemViewModel, CancellationToken, Task> Restore { get; } = restore;

    internal Action<HeldDeletionItemViewModel> DecideLater { get; } = decideLater;
}
