using System.Diagnostics.CodeAnalysis;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWallhavenClientFactory
{
    private static readonly WallhavenConnection Connection = new("api-key", new Uri("https://example.test/"));
    private readonly FakeHttpClientFactory httpClientFactory = new();
    private readonly WallhavenClientFactory factory;

    public GivenAWallhavenClientFactory() => factory = new(httpClientFactory);

    [Fact]
    public void when_a_client_is_created_then_it_is_the_named_wallhaven_client_pointed_at_the_connection()
    {
        using var client = factory.Create(Connection);

        (httpClientFactory.RequestedNames.Single(), client.BaseAddress, client.DefaultRequestHeaders.Referrer).ShouldBe((ApplicationConstants.WallhavenHttpClientName, Connection.BaseUrl, Connection.BaseUrl));
    }

    [Fact]
    public void when_a_client_is_created_then_the_api_key_is_sent_in_the_x_api_key_header()
    {
        using var client = factory.Create(Connection);

        client.DefaultRequestHeaders.GetValues("X-API-Key").ShouldBe(["api-key"]);
    }

    [Fact]
    public void when_two_clients_are_created_then_the_second_does_not_inherit_the_first_ones_api_key()
    {
        using var first = factory.Create(new WallhavenConnection("first-key", new Uri("https://first.test/")));
        using var second = factory.Create(new WallhavenConnection("second-key", new Uri("https://second.test/")));

        (string.Join(",", second.DefaultRequestHeaders.GetValues("X-API-Key")), second.BaseAddress).ShouldBe(("second-key", new Uri("https://second.test/")));
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public List<string> RequestedNames { get; } = [];

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller disposes the client.")]
        public HttpClient CreateClient(string name)
        {
            RequestedNames.Add(name);

            return new HttpClient();
        }
    }
}
