using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Default <see cref="IMsalClient"/> implementation that delegates to an
///     <see cref="IPublicClientApplication"/> configured for personal Microsoft
///     accounts (<c>consumers</c> tenant, AU-01).
/// </summary>
public sealed class MsalClient : IMsalClient
{
    private readonly IPublicClientApplication _app;

    /// <summary>
    ///     Initialises a new instance of <see cref="MsalClient"/> wrapping
    ///     the supplied <paramref name="app"/>.
    /// </summary>
    public MsalClient(IPublicClientApplication app)
        => _app = app;

    /// <inheritdoc />
    public Task<IEnumerable<IAccount>> GetAccountsAsync()
        => _app.GetAccountsAsync();

    /// <inheritdoc />
    public async Task<string> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount? account, CancellationToken cancellationToken = default)
    {
        var result = await _app.AcquireTokenSilent(scopes, account)
                               .ExecuteAsync(cancellationToken)
                               .ConfigureAwait(false);

        return result.AccessToken;
    }

    /// <inheritdoc />
    public async Task<string> AcquireTokenInteractiveAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        var result = await _app.AcquireTokenInteractive(scopes)
                               .ExecuteAsync(cancellationToken)
                               .ConfigureAwait(false);

        return result.AccessToken;
    }

    /// <inheritdoc />
    public Task RemoveAccountAsync(IAccount account)
        => _app.RemoveAsync(account);
}
