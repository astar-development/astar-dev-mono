namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Abstraction for the dialog presented to the user before falling back to
///     the insecure local token-cache store when the OS keychain is unavailable (AU-03).
/// </summary>
/// <remarks>
///     Decoupling the prompt from the cache initialiser allows the UI layer to
///     supply a platform-appropriate dialog (Avalonia message box, console prompt, etc.)
///     while keeping the authentication logic independently testable.
/// </remarks>
public interface IConsentPrompt
{
    /// <summary>
    ///     Displays a consent dialog to the user explaining that the OS keychain
    ///     is unavailable and requesting permission to use an encrypted local
    ///     fallback store instead.
    /// </summary>
    /// <param name="message">The explanation to show the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     <c>true</c> if the user grants consent; <c>false</c> if the user declines.
    /// </returns>
    Task<bool> RequestConsentAsync(string message, CancellationToken cancellationToken = default);
}
