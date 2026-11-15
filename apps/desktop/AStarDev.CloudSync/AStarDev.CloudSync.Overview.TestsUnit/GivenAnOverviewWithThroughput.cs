using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Overview.TestsUnit;

public class GivenAnOverviewWithThroughput
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task when_samples_exist_then_the_graph_shows_the_last_two_hours_and_the_bytes_moved_today_add_up_todays_samples()
    {
        var reader = new FakeThroughputReader
        {
            Samples = Result.Success<IReadOnlyList<TransferSample>, ErrorResponse>(
            [
                Sample(Now.AddHours(-16), 9_999),
                Sample(Now.AddHours(-14.5), 3_000),
                Sample(Now.AddHours(-1.5), 1_000),
                Sample(Now.AddMinutes(-10), 5_000),
            ])
        };

        var overview = await ReadAsync(reader, new RecordingLoggerProvider());

        overview.Throughput.Select(sample => (sample.At, sample.BytesPerSecond)).ShouldBe([(Now.AddHours(-1.5), 100), (Now.AddMinutes(-10), 500)]);
        overview.BytesMovedToday.ShouldBe(9_000);
    }

    [Fact]
    public async Task when_a_sample_took_less_than_a_second_then_its_rate_is_its_bytes()
    {
        var reader = new FakeThroughputReader { Samples = Result.Success<IReadOnlyList<TransferSample>, ErrorResponse>([TransferSampleFactory.Create(Now.AddMinutes(-1), 122, TimeSpan.Zero)]) };

        var overview = await ReadAsync(reader, new RecordingLoggerProvider());

        overview.Throughput.Select(sample => sample.BytesPerSecond).ShouldBe([122]);
    }

    [Fact]
    public async Task when_nothing_was_sampled_then_there_is_no_throughput_and_nothing_moved_today()
    {
        var overview = await ReadAsync(new FakeThroughputReader(), new RecordingLoggerProvider());

        (overview.Throughput.Count, overview.BytesMovedToday).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_samples_cannot_be_read_then_the_overview_still_loads_with_no_throughput_and_a_warning_without_details()
    {
        using var logs = new RecordingLoggerProvider();
        var reader = new FakeThroughputReader { Samples = Result.Failure<IReadOnlyList<TransferSample>, ErrorResponse>(new ErrorResponse("jane@example.com")) };

        var overview = await ReadAsync(reader, logs);

        (overview.Throughput.Count, overview.BytesMovedToday).ShouldBe((0, 0));
        string.Join("\n", logs.Logger.Messages).ShouldContain("throughput");
        string.Join("\n", logs.Logger.Messages).ShouldNotContain("jane@example.com");
    }

    private static TransferSample Sample(DateTimeOffset at, long bytes) => TransferSampleFactory.Create(at, bytes, TimeSpan.FromSeconds(10));

    private static async Task<SyncOverview> ReadAsync(FakeThroughputReader reader, RecordingLoggerProvider logs)
    {
        using var provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FakeTimeProvider(Now))
            .AddSingleton<ICloudSyncDatabase>(new FakeCloudSyncDatabase())
            .AddSingleton<IAccountStore>(new FakeAccountStore())
            .AddSingleton<IAccountSignInStates>(new FakeAccountSignInStates())
            .AddSingleton<ISyncPairReader>(new FakeSyncPairReader())
            .AddSingleton<ISyncRunStatusSource>(new FakeSyncRunStatusSource())
            .AddSingleton<IAccountPauseReader>(new FakeAccountPauseReader())
            .AddSingleton<ISyncBaselineCounter>(new FakeSyncBaselineCounter())
            .AddSingleton<IActivityLogReader>(new FakeActivityLogReader())
            .AddSingleton<ISyncRunJournal>(new InMemorySyncRunJournal())
            .AddSingleton<IAccountThrottleHints>(new FakeAccountThrottleHints())
            .AddSingleton<IThroughputReader>(reader)
            .AddLogging(logging => logging.ClearProviders().AddProvider(logs))
            .AddSingleton<ISyncAttentionReader>(new FakeAttentionReader())
            .AddSyncOverview()
            .BuildServiceProvider();

        return await provider.GetRequiredService<ISyncOverviewSource>().GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(read => read, error => throw new InvalidOperationException(error.Message));
    }
}
