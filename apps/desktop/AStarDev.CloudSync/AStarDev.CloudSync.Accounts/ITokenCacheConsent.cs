namespace AStarDev.CloudSync.Accounts;

/// <summary>Asks the user whether the sign-in tokens may be kept in a plaintext file when the secure token store is unavailable.</summary>
public interface ITokenCacheConsent
{
    /// <summary>Asks the user for consent to use a plaintext token file. It may be called from a background thread, so an implementation that shows a dialog must switch to the UI thread itself.</summary>
    /// <param name="cancellationToken">Cancels the question.</param>
    /// <returns>The user's answer.</returns>
    Task<PlaintextCacheConsent> AskAsync(CancellationToken cancellationToken);
}
