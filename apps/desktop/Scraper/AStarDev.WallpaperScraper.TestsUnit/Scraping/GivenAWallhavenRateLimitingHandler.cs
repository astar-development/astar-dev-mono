using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using AStarDev.WallpaperScraper.Scraping;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenAWallhavenRateLimitingHandler : IDisposable
{
    private static readonly Uri ApiUrl = new("https://wallhaven.cc/api/v1/search?page=2");
    private static readonly Uri ImageUrl = new("https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg");
    private readonly CountingRateLimiter limiter = new();
    private readonly ManualTimeProvider timeProvider = new();
    private readonly StubHandler inner = new();
    private readonly RateLimitBackoffNotifier notifier = new();

    [Fact]
    public async Task when_an_api_request_is_sent_then_one_permit_is_acquired_and_the_response_is_returned()
    {
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        using var response = await client.GetAsync(ApiUrl, CancellationToken.None);

        (response.StatusCode, limiter.AcquiredPermits, inner.RequestedUrls.Single()).ShouldBe((HttpStatusCode.OK, 1, ApiUrl));
    }

    [Fact]
    public async Task when_an_image_request_is_sent_then_no_permit_is_acquired()
    {
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        using var response = await client.GetAsync(ImageUrl, CancellationToken.None);

        (response.StatusCode, limiter.AcquiredPermits, inner.RequestedUrls.Single()).ShouldBe((HttpStatusCode.OK, 0, ImageUrl));
    }

    [Fact]
    public async Task when_the_response_is_not_a_429_then_it_is_returned_without_retrying()
    {
        inner.Enqueue(HttpStatusCode.InternalServerError);
        using var client = CreateClient();

        using var response = await client.GetAsync(ApiUrl, CancellationToken.None);

        (response.StatusCode, inner.RequestedUrls.Count, timeProvider.Delays.Count).ShouldBe((HttpStatusCode.InternalServerError, 1, 0));
    }

    [Fact]
    public async Task when_a_429_carries_a_retry_after_delta_then_the_request_is_retried_after_that_delay()
    {
        inner.Enqueue(HttpStatusCode.TooManyRequests, retryAfter: new RetryConditionHeaderValue(TimeSpan.FromSeconds(5)));
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        var request = client.GetAsync(ApiUrl, CancellationToken.None);
        await timeProvider.WaitForDelaysAsync(1);
        timeProvider.Delays.ShouldBe([TimeSpan.FromSeconds(5)]);
        timeProvider.FireAll();
        using var response = await request;

        (response.StatusCode, inner.RequestedUrls.Count, limiter.AcquiredPermits).ShouldBe((HttpStatusCode.OK, 2, 2));
    }

    [Fact]
    public async Task when_a_429_is_retried_then_the_back_off_delay_is_announced_before_waiting()
    {
        var announced = new List<TimeSpan>();
        notifier.BackingOff += (_, delay) => announced.Add(delay);
        inner.Enqueue(HttpStatusCode.TooManyRequests, retryAfter: new RetryConditionHeaderValue(TimeSpan.FromSeconds(5)));
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        var request = client.GetAsync(ApiUrl, CancellationToken.None);
        await timeProvider.WaitForDelaysAsync(1);
        var announcedBeforeRetry = announced.ToList();
        timeProvider.FireAll();
        using var response = await request;

        announcedBeforeRetry.ShouldBe([TimeSpan.FromSeconds(5)]);
    }

    [Fact]
    public async Task when_the_request_is_not_rate_limited_then_no_back_off_is_announced()
    {
        var announced = 0;
        notifier.BackingOff += (_, _) => announced++;
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        using var response = await client.GetAsync(ApiUrl, CancellationToken.None);

        announced.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_429_has_no_retry_after_then_the_request_is_retried_after_the_default_delay()
    {
        inner.Enqueue(HttpStatusCode.TooManyRequests);
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        var request = client.GetAsync(ApiUrl, CancellationToken.None);
        await timeProvider.WaitForDelaysAsync(1);
        timeProvider.FireAll();
        using var response = await request;

        (timeProvider.Delays.Single(), response.StatusCode).ShouldBe((WallhavenRateLimitingHandler.DefaultRetryDelay, HttpStatusCode.OK));
    }

    [Fact]
    public async Task when_a_429_carries_a_retry_after_date_then_the_request_is_retried_after_the_time_remaining()
    {
        inner.Enqueue(HttpStatusCode.TooManyRequests, retryAfter: new RetryConditionHeaderValue(timeProvider.GetUtcNow().AddSeconds(30)));
        inner.Enqueue(HttpStatusCode.OK);
        using var client = CreateClient();

        var request = client.GetAsync(ApiUrl, CancellationToken.None);
        await timeProvider.WaitForDelaysAsync(1);
        timeProvider.FireAll();
        using var response = await request;

        timeProvider.Delays.ShouldBe([TimeSpan.FromSeconds(30)]);
    }

    [Fact]
    public async Task when_every_attempt_is_a_429_then_the_final_429_is_returned_after_the_maximum_retries()
    {
        for (var attempt = 0; attempt <= WallhavenRateLimitingHandler.MaximumRetries; attempt++)
        {
            inner.Enqueue(HttpStatusCode.TooManyRequests);
        }

        using var client = CreateClient();

        var request = client.GetAsync(ApiUrl, CancellationToken.None);
        for (var retry = 1; retry <= WallhavenRateLimitingHandler.MaximumRetries; retry++)
        {
            await timeProvider.WaitForDelaysAsync(retry);
            timeProvider.FireAll();
        }

        using var response = await request;

        (response.StatusCode, inner.RequestedUrls.Count).ShouldBe((HttpStatusCode.TooManyRequests, WallhavenRateLimitingHandler.MaximumRetries + 1));
    }

    [Fact]
    public async Task when_the_request_is_cancelled_while_waiting_to_retry_then_the_cancellation_is_propagated()
    {
        inner.Enqueue(HttpStatusCode.TooManyRequests);
        using var client = CreateClient();
        using var cancellationTokenSource = new CancellationTokenSource();

        var request = client.GetAsync(ApiUrl, cancellationTokenSource.Token);
        await timeProvider.WaitForDelaysAsync(1);
        await cancellationTokenSource.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => request);
    }

    public void Dispose()
    {
        limiter.Dispose();
        inner.Dispose();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private HttpClient CreateClient() => new(new WallhavenRateLimitingHandler(limiter, notifier, timeProvider) { InnerHandler = inner });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new();

        public List<Uri> RequestedUrls { get; } = [];

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The response is disposed by the caller of the handler.")]
        public void Enqueue(HttpStatusCode statusCode, RetryConditionHeaderValue? retryAfter = null)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Headers.RetryAfter = retryAfter;
            responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrls.Add(request.RequestUri!);

            return Task.FromResult(responses.Dequeue());
        }
    }

    private sealed class CountingRateLimiter : RateLimiter
    {
        public override TimeSpan? IdleDuration => null;

        public int AcquiredPermits { get; private set; }

        public override RateLimiterStatistics? GetStatistics() => null;

        protected override RateLimitLease AttemptAcquireCore(int permitCount) => Acquire(permitCount);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The lease is disposed by the handler under test.")]
        [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "Test double completes synchronously.")]
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) => ValueTask.FromResult(Acquire(permitCount));

        private RateLimitLease Acquire(int permitCount)
        {
            AcquiredPermits += permitCount;

            return new GrantedLease();
        }

        private sealed class GrantedLease : RateLimitLease
        {
            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames => [];

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                metadata = null;

                return false;
            }
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        private readonly List<ManualTimer> timers = [];

        public List<TimeSpan> Delays { get; } = [];

        public override DateTimeOffset GetUtcNow() => Now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(() => callback(state));
            Delays.Add(dueTime);
            timers.Add(timer);

            return timer;
        }

        public async Task WaitForDelaysAsync(int count)
        {
            for (var attempt = 0; attempt < 1000 && Delays.Count < count; attempt++)
            {
                await Task.Delay(1);
            }

            Delays.Count.ShouldBeGreaterThanOrEqualTo(count);
        }

        public void FireAll()
        {
            var pending = timers.ToList();
            timers.Clear();
            foreach (var timer in pending)
            {
                timer.Fire();
            }
        }
    }

    [SuppressMessage("Design", "CA1001", Justification = "Test double with nothing to dispose.")]
    private sealed class ManualTimer(Action onFire) : ITimer
    {
        public void Fire() => onFire();

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
