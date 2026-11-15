using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts and times every folder tree listing (#1747).</summary>
/// <param name="inner">Lists the folders.</param>
/// <param name="telemetry">Records the listing.</param>
public sealed class InstrumentedFolderTreeSource(IFolderTreeSource inner, IWorkflowTelemetry telemetry) : IFolderTreeSource
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> GetChildrenAsync(AccountId accountId, FolderTreePosition parent, CancellationToken cancellationToken) =>
        telemetry.TrackAsync(TelemetryWorkflowFactory.CreateFolderBrowse(), () => inner.GetChildrenAsync(accountId, parent, cancellationToken), result => result.Match(_ => TelemetryOutcomeFactory.CreateSucceeded(), _ => TelemetryOutcomeFactory.CreateFailed()));
}
