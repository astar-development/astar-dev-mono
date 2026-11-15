using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts and times every sign-in again (#1747); the other account commands pass straight through.</summary>
/// <param name="inner">Runs the commands.</param>
/// <param name="telemetry">Records the sign-ins.</param>
public sealed class InstrumentedAccountActions(IAccountActions inner, IWorkflowTelemetry telemetry) : IAccountActions
{
    /// <inheritdoc />
    public Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken) => inner.RemoveAsync(accountId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken) =>
        telemetry.TrackAsync(TelemetryWorkflowFactory.CreateSignIn(), () => inner.ReauthenticateAsync(accountId, cancellationToken), result => result.Match(_ => TelemetryOutcomeFactory.CreateSucceeded(), OutcomeOf));

    private static TelemetryOutcome OutcomeOf(ReauthFailure failure) => failure is ReauthFailure.Cancelled ? TelemetryOutcomeFactory.CreateCancelled() : TelemetryOutcomeFactory.CreateFailed();

    /// <inheritdoc />
    public Task<Result<SyncRoot, SyncRootRejection>> ChangeSyncRootAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken) => inner.ChangeSyncRootAsync(accountId, candidate, cancellationToken);
}
