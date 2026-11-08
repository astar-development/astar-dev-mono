
namespace AStarDev.CloudSync.Accounts;

/// <summary>Creates <see cref="AccountId"/> values.</summary>
public static class AccountIdFactory
{
    /// <summary>Creates the account id that wraps <paramref name="value"/>. The caller supplies the value, so this factory stays deterministic.</summary>
    /// <param name="value">The unique value, for example from <see cref="Guid.NewGuid"/>.</param>
    public static AccountId Create(Guid value) => new(value);
}
