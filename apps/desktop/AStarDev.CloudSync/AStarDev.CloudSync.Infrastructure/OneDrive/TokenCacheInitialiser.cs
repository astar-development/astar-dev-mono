using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to storage once. It prefers the secure store and uses a plaintext file only with the user's consent.</summary>
/// <param name="storage">Connects the cache to the secure store or the plaintext file.</param>
/// <param name="consent">Asks the user before the plaintext file is used.</param>
/// <param name="logger">Reports the storage chosen and failures, without paths or account details.</param>
public sealed class TokenCacheInitialiser(IMsalTokenCacheStorage storage, ITokenCacheConsent consent, ILogger<TokenCacheInitialiser> logger)
{
    /// <summary>Connects the token cache, unless an earlier call already did.</summary>
    /// <param name="cancellationToken">Cancels the consent question.</param>
    /// <returns><see cref="Unit"/> once the cache is connected, or <see cref="AccountAddFailure.TokenCacheUnavailable"/>.</returns>
    public Task<Result<Unit, AccountAddFailure>> EnsureAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{storage}{consent}{logger}{cancellationToken}");
}
