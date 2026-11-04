namespace AStarDev.CloudSync.Core;

/// <summary>Creates <see cref="AccountId"/> values.</summary>
public static class AccountIdFactory
{
    /// <summary>Creates a new, unique account id.</summary>
    public static AccountId Create() => new(Guid.NewGuid());
}
