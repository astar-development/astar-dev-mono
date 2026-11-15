using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts and times every add-account sign-in (#1747).</summary>
/// <param name="inner">Adds the account.</param>
/// <param name="telemetry">Records the attempt.</param>
public sealed class InstrumentedAccountAdder(IAccountAdder inner, IWorkflowTelemetry telemetry) : IAccountAdder
{
    /// <inheritdoc />
    public Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken) =>
        telemetry.TrackAsync(TelemetryWorkflowFactory.CreateSignIn(), () => inner.AddAsync(cancellationToken), OutcomeOf);

    private static TelemetryOutcome OutcomeOf(AccountAddOutcome outcome) => outcome switch
    {
        AccountAddOutcome.Cancelled => TelemetryOutcomeFactory.CreateCancelled(),
        AccountAddOutcome.Failed => TelemetryOutcomeFactory.CreateFailed(),
        _ => TelemetryOutcomeFactory.CreateSucceeded(),
    };
}
