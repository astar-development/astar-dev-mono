using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Client.Infrastructure;

/// <inheritdoc />
internal sealed class GraphClientFactory : IGraphClientFactory
{
    /// <inheritdoc />
    public GraphServiceClient Create(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var tokenProvider = new StaticTokenProvider(accessToken);
        var authProvider  = new BaseBearerTokenAuthenticationProvider(tokenProvider);

        return new GraphServiceClient(authProvider);
    }

    private sealed class StaticTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(accessToken);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
