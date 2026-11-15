using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Turns the progress the passes report into transfer samples (#1767): the bytes moved across every account are added up and written when <see cref="ThroughputLimits.Window"/> has passed since the first of them, and when a phase finishes. A transfer that stops early loses the bytes since its last sample. A sample that cannot be written is warned about and never fails the transfer.</summary>
/// <param name="writer">Stores the samples.</param>
/// <param name="timeProvider">Times the windows.</param>
/// <param name="logger">Reports a write that failed.</param>
internal sealed class ThroughputRecorder(IThroughputWriter writer, TimeProvider timeProvider, ILogger<ThroughputRecorder> logger)
{
    private const string Location = nameof(ThroughputRecorder);

    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, long> bytesDone = [];
    private long pendingBytes;
    private long windowStart;

    /// <summary>Counts the bytes the progress adds to what the account last reported and writes a sample when the window is due.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="progress">The progress; its bytes are cumulative for the phase.</param>
    public void Record(AccountId accountId, SyncProgress progress) => Due(accountId, progress).Match(Write, () => Unit.Instance);

    private Option<TransferSample> Due(AccountId accountId, SyncProgress progress)
    {
        lock (gate)
        {
            var last = bytesDone.GetValueOrDefault(accountId);
            bytesDone[accountId] = progress.BytesDone;
            var added = progress.BytesDone >= last ? progress.BytesDone - last : progress.BytesDone;

            if (added == 0)
                return Option.None<TransferSample>();

            var now = timeProvider.GetTimestamp();
            if (pendingBytes == 0)
                windowStart = now;

            pendingBytes += added;
            var elapsed = timeProvider.GetElapsedTime(windowStart, now);
            var phaseFinished = progress.BytesTotal > 0 && progress.BytesDone >= progress.BytesTotal;

            if (elapsed < ThroughputLimits.Window && !phaseFinished)
                return Option.None<TransferSample>();

            var sample = TransferSampleFactory.Create(timeProvider.GetUtcNow(), pendingBytes, elapsed);
            pendingBytes = 0;

            return Option.Some(sample);
        }
    }

    private Unit Write(TransferSample sample)
    {
        _ = WriteAsync(sample);

        return Unit.Instance;
    }

    private Task<Unit> WriteAsync(TransferSample sample) => writer.AppendAsync(sample, CancellationToken.None).MatchAsync(_ => Unit.Instance, _ => Warn());

    private Unit Warn()
    {
        LogMessage.Warning(logger, Location, "A transfer throughput sample could not be written.");

        return Unit.Instance;
    }
}
