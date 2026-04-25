using Microsoft.Graph;
using System.Net.Http.Headers;

namespace AnotherOneDriveSync.Core;

public class GraphClientFactory : IGraphClientFactory
{
    private readonly IAuthService _authService;

    public GraphClientFactory(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<GraphServiceClient> CreateAsync()
    {
        var authResult = await _authService.AcquireTokenAsync();
        var authProvider = new DelegateAuthenticationProvider(requestMessage =>
        {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            return Task.CompletedTask;
        });
        return new GraphServiceClient(authProvider);
    }
}
