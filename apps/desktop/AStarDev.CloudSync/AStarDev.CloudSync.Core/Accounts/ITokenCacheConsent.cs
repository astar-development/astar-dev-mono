namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>Asks the user whether the sign-in tokens may be kept in a plaintext file when the secure token store is unavailable.</summary>
public interface ITokenCacheConsent
{
    /// <summary>Asks the user for consent to use a plaintext token file.</summary>
    /// <param name="cancellationToken">Cancels the question.</param>
    /// <returns>The user's answer.</returns>
    Task<PlaintextCacheConsent> AskAsync(CancellationToken cancellationToken);
}
