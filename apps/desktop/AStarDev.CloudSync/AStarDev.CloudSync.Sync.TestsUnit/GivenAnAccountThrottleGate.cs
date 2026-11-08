using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnAccountThrottleGate : IDisposable
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId Work = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider provider;

    public GivenAnAccountThrottleGate() => provider = new ServiceCollection().AddSingleton<TimeProvider>(time).AddCloudSync().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IAccountThrottleGate Gate => provider.GetRequiredService<IAccountThrottleGate>();

    [Fact]
    public async Task when_the_account_was_never_throttled_then_waiting_returns_at_once()
    {
        var waiting = Gate.WaitAsync(Personal, CancellationToken);

        await waiting.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        waiting.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_provider_sent_retry_after_then_the_account_waits_exactly_that_long()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromSeconds(3)));

        var waiting = Gate.WaitAsync(Personal, CancellationToken);
        time.Advance(TimeSpan.FromSeconds(2.9));
        var pendingBefore = !waiting.IsCompleted;
        time.Advance(TimeSpan.FromSeconds(0.1));
        await waiting.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        pendingBefore.ShouldBeTrue();
    }

    [Fact]
    public async Task when_one_account_is_throttled_then_another_account_is_not_paused()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromMinutes(1)));

        var waiting = Gate.WaitAsync(Work, CancellationToken);
        await waiting.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        waiting.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public void when_throttles_without_retry_after_repeat_then_the_pause_doubles_from_five_seconds_up_to_five_minutes()
    {
        var pauses = Enumerable.Range(0, 9).Select(_ => PauseAfterThrottle(Option.None<TimeSpan>())).ToList();

        pauses.ShouldBe([5, 10, 20, 40, 80, 160, 300, 300, 300]);
    }

    [Fact]
    public void when_a_request_succeeds_then_the_back_off_starts_again_from_five_seconds()
    {
        _ = PauseAfterThrottle(Option.None<TimeSpan>());
        _ = PauseAfterThrottle(Option.None<TimeSpan>());

        Gate.ReportSucceeded(Personal);

        PauseAfterThrottle(Option.None<TimeSpan>()).ShouldBe(5);
    }

    [Fact]
    public void when_retry_after_is_longer_than_ten_minutes_then_the_pause_is_capped_at_ten_minutes()
    {
        var pause = PauseAfterThrottle(Option.Some(TimeSpan.FromHours(1)));

        pause.ShouldBe(600);
    }

    [Fact]
    public void when_retry_after_is_zero_or_negative_then_the_account_is_not_paused()
    {
        var pauses = new[] { PauseAfterThrottle(Option.Some(TimeSpan.Zero)), PauseAfterThrottle(Option.Some(TimeSpan.FromSeconds(-30))) };

        pauses.ShouldBe([0, 0]);
    }

    [Fact]
    public void when_a_shorter_pause_is_reported_during_a_longer_one_then_the_longer_pause_is_kept()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromSeconds(60)));

        var pause = PauseAfterThrottle(Option.Some(TimeSpan.FromSeconds(2)));

        pause.ShouldBe(60);
    }

    [Fact]
    public async Task when_the_caller_cancels_while_paused_then_the_wait_is_cancelled()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromMinutes(1)));
        using var cancellation = new CancellationTokenSource();

        var waiting = Gate.WaitAsync(Personal, cancellation.Token);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => waiting);
    }

    [Fact]
    public async Task when_many_requests_wait_on_a_paused_account_then_all_are_released_together()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromSeconds(10)));

        var waits = Enumerable.Range(0, 20).Select(_ => Task.Run(() => Gate.WaitAsync(Personal, CancellationToken), CancellationToken)).ToList();
        await Task.Delay(100, CancellationToken);
        var nonePassed = waits.All(wait => !wait.IsCompleted);
        time.Advance(TimeSpan.FromSeconds(10));
        await Task.WhenAll(waits).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        nonePassed.ShouldBeTrue();
    }

    public void Dispose() => provider.Dispose();

    private int PauseAfterThrottle(Option<TimeSpan> retryAfter)
    {
        Gate.ReportThrottled(Personal, retryAfter);
        var seconds = 0;
        while (!Gate.WaitAsync(Personal, CancellationToken).IsCompleted)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            seconds++;
        }

        return seconds;
    }
}
