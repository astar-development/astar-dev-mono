using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts an account's files in its local sync folder and, read live from the provider, in its cloud account (#1801). Files only, and only those the sync would consider: ignored paths are left out on both sides. It reads; it never changes the local folder, the remote index, the stored change cursor or the selection rules.</summary>
public interface IFileAuditor
{
    /// <summary>Counts the account's files. The cloud side reads the whole account from the provider, so it can take a long time on a large account.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the audit; the cancellation is thrown, not returned.</param>
    /// <returns>The two counts, or why one of them could not be read. A failure is never a count of zero.</returns>
    Task<Result<FileAuditCounts, FileAuditFailure>> AuditAsync(AccountId accountId, CancellationToken cancellationToken);
}
