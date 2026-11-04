using AStarDev.OneDriveSyncClient.Dashboard.Throughput;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard.Throughput;

public sealed class GivenAThroughputGraphViewModel
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private readonly ISyncThroughputRepository _repository = Substitute.For<ISyncThroughputRepository>();
    private readonly IUiTimer _timer = Substitute.For<IUiTimer>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private ThroughputGraphViewModel CreateSut() => new(_repository, _timer, new FakeTimeProvider(Now), _localizationService, Substitute.For<ILogger<ThroughputGraphViewModel>>());

    private static SyncThroughputSampleEntity Sample(TimeSpan age, long bytes) => new() { AccountId = new AccountId("acc-1"), CompletedAt = Now - age, Bytes = bytes };

    [Fact]
    public void when_created_then_twelve_empty_bars_are_shown()
    {
        var sut = CreateSut();

        sut.Bars.Count.ShouldBe(12);
        sut.Bars.ShouldAllBe(bar => bar.HeightFraction == 0);
    }

    [Fact]
    public void when_started_then_the_timer_runs_every_two_minutes()
    {
        var sut = CreateSut();

        sut.Start();

        _timer.Received(1).Start(TimeSpan.FromMinutes(2), Arg.Any<Action>());
    }

    [Fact]
    public async Task when_refreshed_then_samples_from_the_last_two_hours_are_requested()
    {
        _repository.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        await sut.RefreshAsync(TestContext.Current.CancellationToken);

        await _repository.Received(1).GetSinceAsync(Now.AddHours(-2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_refreshed_then_the_busiest_bucket_is_full_height_and_others_are_proportional()
    {
        _repository.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([Sample(TimeSpan.FromMinutes(1), 400), Sample(TimeSpan.FromMinutes(15), 100)]);
        var sut = CreateSut();

        await sut.RefreshAsync(TestContext.Current.CancellationToken);

        sut.Bars[11].HeightFraction.ShouldBe(1.0);
        sut.Bars[10].HeightFraction.ShouldBe(0.25);
        sut.Bars[0].HeightFraction.ShouldBe(0.0);
    }

    [Fact]
    public async Task when_refreshed_with_no_samples_then_has_data_is_false()
    {
        _repository.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        await sut.RefreshAsync(TestContext.Current.CancellationToken);

        sut.HasData.ShouldBeFalse();
    }

    [Fact]
    public async Task when_refreshed_with_samples_then_has_data_is_true()
    {
        _repository.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([Sample(TimeSpan.FromMinutes(1), 400)]);
        var sut = CreateSut();

        await sut.RefreshAsync(TestContext.Current.CancellationToken);

        sut.HasData.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_repository_throws_then_previous_bars_are_kept()
    {
        _repository.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromResult<IReadOnlyList<SyncThroughputSampleEntity>>([Sample(TimeSpan.FromMinutes(1), 400)]),
            _ => throw new InvalidOperationException("boom"));
        var sut = CreateSut();
        await sut.RefreshAsync(TestContext.Current.CancellationToken);

        await sut.RefreshAsync(TestContext.Current.CancellationToken);

        sut.Bars[11].HeightFraction.ShouldBe(1.0);
    }

    [Fact]
    public void when_the_title_is_requested_then_it_uses_the_dashboard_throughput_title_key()
    {
        _localizationService.GetLocal("Dashboard.ThroughputTitle").Returns("Sync throughput (2h)");
        var sut = CreateSut();

        sut.TitleText.ShouldBe("Sync throughput (2h)");
    }
}
