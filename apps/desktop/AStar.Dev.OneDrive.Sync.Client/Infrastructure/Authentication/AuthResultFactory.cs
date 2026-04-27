namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

public static class AuthResultFactory
{
    extension(AuthResult)
    {
        /// <summary>
        /// Represents a cancelled authentication operation. Consumers can check for this result to handle cancellations gracefully, such as by showing a message to the user or simply returning to the previous state without an error.
        /// </summary>
        /// <returns>An AuthResult representing a cancelled authentication.</returns>
        public static AuthResult Cancelled => new(false, true, null, null, null, null, null);

        /// <summary>
        /// Creates an AuthResult representing a failed authentication attempt, with an optional error message describing the failure. Consumers can use this information to display error messages or take corrective actions.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <returns>An AuthResult representing the failure.</returns>
        public static AuthResult Failure(string errorMessage) => new(false, false, null, null, null, null, errorMessage);

        /// <summary>
        /// Creates an AuthResult representing a successful authentication attempt, with the necessary tokens and user information.
        /// </summary>
        /// <param name="accessToken">The access token for the authenticated user.</param>
        /// <param name="accountId">The account ID of the authenticated user.</param>
        /// <param name="displayName">The display name of the authenticated user.</param>
        /// <param name="email">The email address of the authenticated user.</param>
        /// <returns>An AuthResult representing the success.</returns>
        public static AuthResult Success(string accessToken, string accountId, string displayName, string email)
        {
            ArgumentNullException.ThrowIfNull(accessToken);
            ArgumentNullException.ThrowIfNull(accountId);
            ArgumentNullException.ThrowIfNull(displayName);
            ArgumentNullException.ThrowIfNull(email);
            
            return new(true, false, accessToken, accountId, displayName, email, null);
        }
    }
}
