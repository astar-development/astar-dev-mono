using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="AccountAddAttemptId"/> values.</summary>
public static class AccountAddAttemptIdFactory
{
    /// <summary>Creates an attempt id from a GUID.</summary>
    /// <param name="value">The GUID that identifies the attempt.</param>
    public static AccountAddAttemptId Create(Guid value) => throw new NotImplementedException($"{value}");
}
