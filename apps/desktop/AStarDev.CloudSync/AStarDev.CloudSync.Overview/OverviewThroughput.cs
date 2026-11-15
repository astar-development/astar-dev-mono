using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Overview;

/// <summary>Reads the transfer samples (#1767) for the overview: the samples of the last two hours become the throughput graph and the samples since local midnight add up to the bytes moved today. Samples that cannot be read are warned about without details and shown as none.</summary>
/// <param name="reader">Reads the samples.</param>
/// <param name="timeProvider">Gives the current time and the time zone that decides when today started.</param>
/// <param name="logger">Reports samples that could not be read.</param>
internal sealed class OverviewThroughput(IThroughputReader reader, TimeProvider timeProvider, ILogger<OverviewThroughput> logger)
{
    private static readonly TimeSpan GraphWindow = TimeSpan.FromHours(2);

    /// <summary>Reads the graph samples and the bytes moved today.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task<(IReadOnlyList<ThroughputSample> Samples, long BytesToday)> ReadAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var startOfToday = StartOfToday(now);
        var graphStart = now - GraphWindow;

        return await reader.GetSinceAsync(startOfToday < graphStart ? startOfToday : graphStart, cancellationToken)
            .MatchAsync(samples => Summarise(samples, startOfToday, graphStart), _ => Unreadable())
            .ConfigureAwait(false);
    }

    private static (IReadOnlyList<ThroughputSample> Samples, long BytesToday) Summarise(IReadOnlyList<TransferSample> samples, DateTimeOffset startOfToday, DateTimeOffset graphStart) =>
        ([.. samples.Where(sample => sample.At >= graphStart).Select(ToThroughput)], samples.Where(sample => sample.At >= startOfToday).Sum(sample => sample.Bytes));

    private static ThroughputSample ToThroughput(TransferSample sample) => ThroughputSampleFactory.Create(sample.At, sample.Bytes * 1000 / Math.Max((long)sample.Elapsed.TotalMilliseconds, 1000));

    private DateTimeOffset StartOfToday(DateTimeOffset now)
    {
        var zone = timeProvider.LocalTimeZone;
        var midnight = TimeZoneInfo.ConvertTime(now, zone).Date;

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(zone.IsInvalidTime(midnight) ? midnight.AddHours(1) : midnight, zone), TimeSpan.Zero);
    }

    private (IReadOnlyList<ThroughputSample> Samples, long BytesToday) Unreadable()
    {
        LogMessage.Warning(logger, nameof(OverviewThroughput), "The transfer throughput could not be read and is shown empty.");

        return ([], 0);
    }
}
