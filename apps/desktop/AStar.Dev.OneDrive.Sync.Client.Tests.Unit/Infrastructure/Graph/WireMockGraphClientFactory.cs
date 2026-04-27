using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock.Server;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

internal sealed class WireMockGraphClientFactory(WireMockServer server) : IGraphClientFactory
{
    public GraphServiceClient CreateClient(string accessToken)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new TestTokenProvider(accessToken));
        var adapter = new HttpClientRequestAdapter(authProvider);
        adapter.BaseUrl = server.Url;

        return new GraphServiceClient(adapter);
    }

    private sealed class TestTokenProvider(string token) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken ct = default)
            => Task.FromResult(token);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
