using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

internal sealed class HeldDeletionActions(Func<HeldDeletionItemViewModel, CancellationToken, Task> confirm, Func<HeldDeletionItemViewModel, CancellationToken, Task> restore, Action<HeldDeletionItemViewModel> decideLater, Func<HeldDeletionId, bool> wasExpanded, Action<HeldDeletionId, bool> expandedChanged)
{
    internal Func<HeldDeletionItemViewModel, CancellationToken, Task> Confirm { get; } = confirm;

    internal Func<HeldDeletionItemViewModel, CancellationToken, Task> Restore { get; } = restore;

    internal Action<HeldDeletionItemViewModel> DecideLater { get; } = decideLater;

    internal Func<HeldDeletionId, bool> WasExpanded { get; } = wasExpanded;

    internal Action<HeldDeletionId, bool> ExpandedChanged { get; } = expandedChanged;
}
