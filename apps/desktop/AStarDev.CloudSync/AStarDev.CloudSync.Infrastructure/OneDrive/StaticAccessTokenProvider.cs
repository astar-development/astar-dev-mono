using Microsoft.Kiota.Abstractions.Authentication;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

internal sealed class StaticAccessTokenProvider(string accessToken) : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);

    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default) => Task.FromResult(accessToken);
}
