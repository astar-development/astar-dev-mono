using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Dashboard.Throughput;

/// <summary>Groups throughput samples into equal, consecutive time buckets ending at a given moment.</summary>
public static class ThroughputBucketer
{
    /// <summary>Returns <paramref name="bucketCount"/> byte totals, oldest first, covering the window that ends at <paramref name="now"/>. Samples outside the window are ignored.</summary>
    public static IReadOnlyList<long> Bucket(IReadOnlyList<SyncThroughputSampleEntity> samples, DateTimeOffset now, int bucketCount, TimeSpan bucketSpan)
    {
        var totals = new long[bucketCount];

        foreach (var sample in samples)
        {
            var age = now - sample.CompletedAt;
            if (age < TimeSpan.Zero || age >= bucketSpan * bucketCount)
                continue;

            totals[bucketCount - 1 - (int)(age / bucketSpan)] += sample.Bytes;
        }

        return totals;
    }
}
