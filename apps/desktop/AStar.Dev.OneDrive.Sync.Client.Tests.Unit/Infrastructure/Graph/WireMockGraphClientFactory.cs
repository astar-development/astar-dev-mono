using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

internal sealed class WireMockGraphClientFactory(WireMockServer server) : IGraphClientFactory
{
    public GraphServiceClient CreateClient(Func<CancellationToken, Task<string>> tokenFactory)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new DelegatingTokenProvider(tokenFactory));
        var adapter = new HttpClientRequestAdapter(authProvider)
        {
            BaseUrl = server.Url
        };

        return new GraphServiceClient(adapter);
    }

    private sealed class DelegatingTokenProvider(Func<CancellationToken, Task<string>> tokenFactory) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken ct = default)
            => tokenFactory(ct);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
