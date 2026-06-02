namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>
/// Wraps <see cref="IAuthService"/> with an in-memory token cache so that
/// <see cref="IAuthService.AcquireTokenSilentAsync"/> is only called when the current
/// token is within <see cref="TokenRefreshThreshold"/> of expiry — not on every Graph request.
///
/// This resolves the conflict between:
///   - Static token capture (causes IDX14100 for syncs lasting over ~1 hour)
///   - Per-request MSAL call (causes excessive libsecret keyring reads on Linux → invalid_grant)
/// </summary>
internal sealed class CachedTokenFactory : IDisposable
{
    private static readonly TimeSpan TokenRefreshThreshold = TimeSpan.FromMinutes(5);

    private readonly string accountId;
    private readonly IAuthService authService;
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private string cachedToken;
    private DateTimeOffset tokenExpiresOn;

    internal CachedTokenFactory(string accountId, IAuthService authService, string initialToken, DateTimeOffset initialExpiresOn)
    {
        this.accountId = accountId;
        this.authService = authService;
        cachedToken = initialToken;
        tokenExpiresOn = initialExpiresOn;
    }

    internal async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (!IsNearExpiry())
            return cachedToken;

        await refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!IsNearExpiry())
                return cachedToken;

            var refreshResult = await authService.AcquireTokenSilentAsync(accountId, ct).ConfigureAwait(false);
            (cachedToken, tokenExpiresOn) = refreshResult.Match<(string, DateTimeOffset)>(
                ok => (ok.AccessToken, ok.ExpiresOn),
                _ => (cachedToken, tokenExpiresOn));

            return cachedToken;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => refreshLock.Dispose();

    private bool IsNearExpiry() => tokenExpiresOn - DateTimeOffset.UtcNow < TokenRefreshThreshold;
}
