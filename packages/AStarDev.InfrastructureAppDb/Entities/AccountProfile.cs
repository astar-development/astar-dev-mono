namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the profile information of a OneDrive account, including display name and email address.
/// </summary>
/// <param name="DisplayName">The display name of the account.</param>
/// <param name="Email">The email address of the account.</param>
public sealed record AccountProfile(string DisplayName, string Email);
