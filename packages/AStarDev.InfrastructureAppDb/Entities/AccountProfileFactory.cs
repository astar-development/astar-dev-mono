namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Factory for creating instances of <see cref="AccountProfile"/> with default or specified values.
/// </summary>
public static class AccountProfileFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="AccountProfile"/> with the provided display name and email.
    /// </summary>
    /// <param name="displayName">The display name of the account.</param>
    /// <param name="email">The email address of the account.</param>
    /// <returns>The created <see cref="AccountProfile"/>.</returns>
    public static AccountProfile Create(string displayName, string email) => new(displayName, email);

    /// <summary>
    /// Provides an empty instance of <see cref="AccountProfile"/> with default values for display name and email.
    /// </summary>
    public static AccountProfile Empty => new(string.Empty, string.Empty);
}
