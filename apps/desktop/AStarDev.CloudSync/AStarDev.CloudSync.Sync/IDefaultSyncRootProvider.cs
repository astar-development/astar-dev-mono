using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Proposes the sync root a new account starts with (#1394).</summary>
public interface IDefaultSyncRootProvider
{
    /// <summary>Proposes <c>&lt;home&gt;/CloudSync/&lt;Provider&gt;-&lt;DisplayName&gt;</c>, made safe for a file system. When that crosses an existing root, a numeric suffix is added until it does not.</summary>
    /// <param name="registration">The account being added.</param>
    /// <param name="existing">Every account's current root.</param>
    /// <returns>A root that crosses none of <paramref name="existing"/>, or <see cref="SyncRootRejection.Unavailable"/> when there is no home folder or no free name.</returns>
    Result<SyncRoot, SyncRootRejection> CreateFor(AccountRegistration registration, IReadOnlyList<AccountSyncRoot> existing);
}
