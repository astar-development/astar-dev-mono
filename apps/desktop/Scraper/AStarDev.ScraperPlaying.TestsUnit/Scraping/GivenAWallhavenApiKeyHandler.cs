using System.Diagnostics.CodeAnalysis;
using System.Net;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWallhavenApiKeyHandler : IDisposable
{
    private static readonly Uri ApiUrl = new("https://wallhaven.cc/api/v1/search?page=2");
    private static readonly Uri ImageUrl = new("https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg");
    private readonly StubHandler inner = new();

    public void Dispose() => inner.Dispose();

    [Fact]
    public async Task when_an_api_request_carries_the_api_key_then_it_is_sent_unchanged()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(ApiUrl, TestContext.Current.CancellationToken);

        inner.SentApiKeys.ShouldBe(["secret-key"]);
    }

    [Fact]
    public async Task when_an_image_request_carries_the_api_key_then_the_key_is_removed_before_it_is_sent()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(ImageUrl, TestContext.Current.CancellationToken);

        inner.SentApiKeys.ShouldBe([string.Empty]);
    }

    [Fact]
    public async Task when_an_image_request_is_sent_then_its_other_headers_are_kept()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Referrer = new Uri("https://wallhaven.cc/");

        using var response = await client.GetAsync(ImageUrl, TestContext.Current.CancellationToken);

        inner.SentReferrers.ShouldBe(["https://wallhaven.cc/"]);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    public async Task when_a_request_has_no_api_key_then_it_is_sent_as_it_is()
    {
        using var client = new HttpClient(new WallhavenApiKeyHandler { InnerHandler = inner });

        using var response = await client.GetAsync(ImageUrl, TestContext.Current.CancellationToken);

        inner.SentApiKeys.ShouldBe([string.Empty]);
    }

    [Fact]
    public async Task when_the_same_client_sends_an_api_request_and_an_image_request_then_only_the_api_request_carries_the_key()
    {
        using var client = CreateClient();

        using var apiResponse = await client.GetAsync(ApiUrl, TestContext.Current.CancellationToken);
        using var imageResponse = await client.GetAsync(ImageUrl, TestContext.Current.CancellationToken);

        inner.SentApiKeys.ShouldBe(["secret-key", string.Empty]);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private HttpClient CreateClient()
    {
        var client = new HttpClient(new WallhavenApiKeyHandler { InnerHandler = inner });
        client.DefaultRequestHeaders.Add("X-API-Key", "secret-key");

        return client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<string> SentApiKeys { get; } = [];

        public List<string> SentReferrers { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentApiKeys.Add(request.Headers.TryGetValues("X-API-Key", out var values) ? string.Join(",", values) : string.Empty);
            SentReferrers.Add(request.Headers.Referrer?.ToString() ?? string.Empty);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
