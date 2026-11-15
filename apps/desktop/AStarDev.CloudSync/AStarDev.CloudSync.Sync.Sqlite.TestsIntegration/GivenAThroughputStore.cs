using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAThroughputStore : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private static TransferSample SampleAt(DateTimeOffset at, long bytes) => TransferSampleFactory.Create(at, bytes, TimeSpan.FromSeconds(10));

    private async Task AppendAsync(TransferSample sample) => (await Database.ThroughputWriter.AppendAsync(sample, CancellationToken)).Match(_ => "stored", error => throw new InvalidOperationException(error.Message)).ShouldBe("stored");

    private async Task<IReadOnlyList<TransferSample>> SinceAsync(DateTimeOffset since) => (await Database.ThroughputReader.GetSinceAsync(since, CancellationToken)).Match(found => found, error => throw new InvalidOperationException(error.Message));

    [Fact]
    public async Task when_nothing_was_sampled_then_nothing_is_read()
    {
        (await SinceAsync(Start.AddDays(-1))).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_sample_is_appended_then_it_is_read_back_unchanged()
    {
        var sample = TransferSampleFactory.Create(Start, 1_234, TimeSpan.FromMilliseconds(2_500));

        await AppendAsync(sample);

        (await SinceAsync(Start.AddHours(-1))).ShouldBe([sample]);
    }

    [Fact]
    public async Task when_samples_are_read_since_a_time_then_only_those_at_or_after_it_are_returned_oldest_first()
    {
        await AppendAsync(SampleAt(Start.AddMinutes(30), 3));
        await AppendAsync(SampleAt(Start, 1));
        await AppendAsync(SampleAt(Start.AddMinutes(10), 2));

        var found = await SinceAsync(Start.AddMinutes(10));

        found.Select(sample => sample.Bytes).ShouldBe([2, 3]);
    }

    [Fact]
    public async Task when_a_sample_is_appended_then_samples_older_than_the_retention_before_it_are_removed()
    {
        await AppendAsync(SampleAt(Start, 1));
        await AppendAsync(SampleAt(Start.AddHours(23), 2));

        await AppendAsync(SampleAt(Start.AddHours(25), 3));

        (await SinceAsync(Start.AddDays(-1))).Select(sample => sample.Bytes).ShouldBe([2, 3]);
    }
}
