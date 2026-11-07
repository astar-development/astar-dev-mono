using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

internal sealed class WireMockGraphClientFactory(WireMockServer server) : IGraphClientFactory
{
    public GraphServiceClient CreateClient(Func<CancellationToken, Task<string>> tokenFactory)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new DelegatingTokenProvider(tokenFactory));
        var httpClient = KiotaClientFactory.Create(new GraphHostRedirectingHandler(new Uri(server.Url!)) { InnerHandler = new HttpClientHandler() });
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = server.Url
        };

        return new GraphServiceClient(adapter);
    }

    private sealed class GraphHostRedirectingHandler(Uri wireMockUrl) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { } requestUri && string.Equals(requestUri.Host, "graph.microsoft.com", StringComparison.OrdinalIgnoreCase))
                request.RequestUri = new UriBuilder(requestUri) { Scheme = wireMockUrl.Scheme, Host = wireMockUrl.Host, Port = wireMockUrl.Port }.Uri;

            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class DelegatingTokenProvider(Func<CancellationToken, Task<string>> tokenFactory) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            => tokenFactory(cancellationToken);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
