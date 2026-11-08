using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnAccountThrottleHandler : IDisposable
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));
    private readonly ScriptedHandler graph = new();
    private readonly ServiceProvider provider;

    public GivenAnAccountThrottleHandler()
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(time).AddCloudSync().AddOneDriveSync();
        _ = services.AddHttpClient(OneDriveSyncHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => graph);
        provider = services.BuildServiceProvider();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IAccountThrottleGate Gate => provider.GetRequiredService<IAccountThrottleGate>();

    [Fact]
    public async Task when_graph_answers_429_with_retry_after_then_the_account_is_paused_for_that_long()
    {
        graph.Next = () => Throttled(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(7));

        using var response = await SendAsync(Option.Some(Personal));

        (response.StatusCode, PausedSeconds()).ShouldBe((HttpStatusCode.TooManyRequests, 7));
    }

    [Fact]
    public async Task when_graph_answers_503_with_a_retry_after_date_then_the_account_is_paused_until_then()
    {
        graph.Next = () => ThrottledUntil(HttpStatusCode.ServiceUnavailable, time.GetUtcNow().AddSeconds(12));

        using var response = await SendAsync(Option.Some(Personal));

        PausedSeconds().ShouldBe(12);
    }

    [Fact]
    public async Task when_graph_answers_503_without_retry_after_then_the_account_backs_off_five_seconds()
    {
        graph.Next = () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        using var response = await SendAsync(Option.Some(Personal));

        PausedSeconds().ShouldBe(5);
    }

    [Fact]
    public async Task when_the_account_is_paused_then_the_request_is_not_sent_until_the_pause_ends()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromSeconds(4)));

        var sending = SendAsync(Option.Some(Personal));
        await Task.Delay(100, CancellationToken);
        var sentEarly = graph.Requests;
        time.Advance(TimeSpan.FromSeconds(4));
        using var response = await sending.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (sentEarly, graph.Requests).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_a_request_succeeds_then_the_back_off_is_reset()
    {
        Gate.ReportThrottled(Personal, Option.None<TimeSpan>());
        time.Advance(TimeSpan.FromSeconds(5));
        Gate.ReportThrottled(Personal, Option.None<TimeSpan>());
        time.Advance(TimeSpan.FromSeconds(10));
        graph.Next = () => new HttpResponseMessage(HttpStatusCode.OK);

        using var response = await SendAsync(Option.Some(Personal));
        Gate.ReportThrottled(Personal, Option.None<TimeSpan>());

        PausedSeconds().ShouldBe(5);
    }

    [Fact]
    public async Task when_a_request_names_no_account_then_it_is_sent_and_nothing_is_paused()
    {
        Gate.ReportThrottled(Personal, Option.Some(TimeSpan.FromMinutes(1)));
        graph.Next = () => Throttled(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(30));

        using var response = await SendAsync(Option.None<AccountId>()).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (response.StatusCode, graph.Requests).ShouldBe((HttpStatusCode.TooManyRequests, 1));
    }

    public void Dispose()
    {
        provider.Dispose();
        graph.Dispose();
    }

    private static HttpResponseMessage Throttled(HttpStatusCode status, TimeSpan retryAfter)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter);

        return response;
    }

    private static HttpResponseMessage ThrottledUntil(HttpStatusCode status, DateTimeOffset until)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(until);

        return response;
    }

    private async Task<HttpResponseMessage> SendAsync(Option<AccountId> account)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://graph.microsoft.com/v1.0/me/drive"));
        _ = account.Match(accountId => Set(request, accountId), () => Unit.Instance);

        return await provider.GetRequiredService<IHttpClientFactory>().CreateClient(OneDriveSyncHttp.ClientName).SendAsync(request, CancellationToken);
    }

    private static Unit Set(HttpRequestMessage request, AccountId accountId)
    {
        request.Options.Set(OneDriveSyncHttp.AccountOption, accountId);

        return Unit.Instance;
    }

    private int PausedSeconds()
    {
        var seconds = 0;
        while (!Gate.WaitAsync(Personal, CancellationToken).IsCompleted)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            seconds++;
        }

        return seconds;
    }
}
