namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="AccountProfile"/>.</summary>
public static class AccountProfileFactory
{
    /// <summary>Creates an <see cref="AccountProfile"/> from the given Microsoft account display fields.</summary>
    public static AccountProfile Create(string displayName, string email) => new(displayName, email);

    /// <summary>An empty profile with no display fields set.</summary>
    public static AccountProfile Empty => new(string.Empty, string.Empty);
}
