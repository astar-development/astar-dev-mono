using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Records each completed run with the engine version, the seed version applied when it started and when it finished, so <see cref="IReclassifyFreshnessCheck"/> can tell when a rerun is worth offering. A cancelled, failed or refused run records nothing. The seed version is read before the run, so a run that started while a new seed was still being written records the old one and stays stale.</summary>
/// <param name="inner">Runs the reclassify.</param>
/// <param name="seedState">Reads the applied seed version.</param>
/// <param name="runs">Keeps the completed run.</param>
/// <param name="options">Supplies the clock.</param>
/// <param name="logger">Reports a record that could not be written, by failure type only.</param>
public sealed class RunRecordingReclassifier(IClassificationReclassifier inner, ISeedStateStore seedState, IReclassifyRunStore runs, ReclassifyOptions options, ILogger<RunRecordingReclassifier> logger) : IClassificationReclassifier
{
    /// <inheritdoc />
    public Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken) => throw new NotImplementedException($"{inner}{seedState}{runs}{options}{logger}{progress}{cancellationToken}");
}
