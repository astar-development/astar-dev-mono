namespace AStarDev.CloudSync.ViewModels;

/// <summary>One row of the layout B synced folders table: the sync pair plus its local path shortened for display.</summary>
/// <param name="pair">The sync pair.</param>
/// <param name="localPathFormatter">Shortens the local path.</param>
public sealed class SyncedFolderViewModel(SyncPairViewModel pair, LocalPathFormatter localPathFormatter)
{
    /// <summary>The sync pair; its <see cref="SyncPairViewModel.LocalPath"/> is the full path for the tooltip.</summary>
    public SyncPairViewModel Pair => pair;

    /// <summary>The local path with the home folder shown as <c>~</c>, e.g. "~/CloudSync/OneDrive/Work/Documents".</summary>
    public string LocalPathText { get; } = localPathFormatter.Format(pair.LocalPath);
}
