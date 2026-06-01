using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public sealed class GraphClientFactory : IGraphClientFactory
{
    /// <inheritdoc />
    public GraphServiceClient CreateClient(Func<CancellationToken, Task<string>> tokenFactory)
        => new(new BaseBearerTokenAuthenticationProvider(new DelegatingAccessTokenProvider(tokenFactory)));

    private sealed class DelegatingAccessTokenProvider(Func<CancellationToken, Task<string>> tokenFactory) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken ct = default)
            => tokenFactory(ct);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
    }
}
