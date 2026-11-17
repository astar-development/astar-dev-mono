using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Records each completed run with the engine version, the seed version applied when it started and when it finished, so <see cref="IReclassifyFreshnessCheck"/> can tell when a rerun is worth offering. A cancelled, failed or refused run records nothing. Every run's progress is teed to the <see cref="ReclassifyProgressSource"/>, and the run's snapshot is cleared when it ends, however it ends (#1906). The seed version is read before the run, so a run that started while a new seed was still being written records the old one and stays stale (#1876). Like the inner reclassifier, it never throws for a cancel: a seed read that fails or is cancelled records no seed version, and a record write that fails leaves the outcome as it was. The record is written with <see cref="CancellationToken.None"/>: the run's work is already done, and a cancel or shutdown in the gap must not drop the one-row write and leave a full run looking stale.</summary>
/// <param name="inner">Runs the reclassify.</param>
/// <param name="seedState">Reads the applied seed version.</param>
/// <param name="runs">Keeps the completed run.</param>
/// <param name="options">Supplies the clock.</param>
/// <param name="progressSource">Shows the progress of the run to the pages, whoever started it (#1906).</param>
/// <param name="logger">Reports a record that could not be written, by failure type only.</param>
public sealed class RunRecordingReclassifier(IClassificationReclassifier inner, ISeedStateStore seedState, IReclassifyRunStore runs, ReclassifyOptions options, ReclassifyProgressSource progressSource, ILogger<RunRecordingReclassifier> logger) : IClassificationReclassifier, IReclassifyRunRecordedSource
{
    /// <inheritdoc />
    public event EventHandler? RunRecorded;

    /// <inheritdoc />
    public Task WaitUntilIdleAsync(CancellationToken cancellationToken) => inner.WaitUntilIdleAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken)
    {
        var seedVersion = await AppliedSeedAsync(cancellationToken).ConfigureAwait(false);
        ReclassifyOutcome outcome;
        using (var scope = progressSource.Begin(progress))
            outcome = await inner.StartAsync(scope, cancellationToken).ConfigureAwait(false);

        if (outcome is ReclassifyOutcome.Completed)
            await RecordAsync(ReclassifyRunRecordFactory.Create(ClassificationEngineVersion.Current, seedVersion, options.TimeProvider.GetUtcNow())).ConfigureAwait(false);

        return outcome;
    }

    private async Task<Option<string>> AppliedSeedAsync(CancellationToken cancellationToken)
    {
        var read = seedState.GetAppliedVersionAsync(cancellationToken).MatchAsync(version => version, _ => Option.None<string>());
        await ((Task)read).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return read.IsCompletedSuccessfully ? read.Result : Option.None<string>();
    }

    private async Task RecordAsync(ReclassifyRunRecord run)
    {
        var write = runs.SetLastCompletedAsync(run, CancellationToken.None).MatchAsync(_ => RecordSucceeded(), RecordFailed);
        await ((Task)write).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private Unit RecordSucceeded()
    {
        RunRecorded?.Invoke(this, EventArgs.Empty);

        return Unit.Instance;
    }

    private Unit RecordFailed(ClassificationFailure failure)
    {
        LogMessage.Warning(logger, nameof(RunRecordingReclassifier), "The completed reclassify run could not be recorded", failure.GetType().Name);

        return Unit.Instance;
    }
}
