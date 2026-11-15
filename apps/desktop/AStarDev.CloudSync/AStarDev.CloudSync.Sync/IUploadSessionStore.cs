using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Keeps the upload session of a large file so an upload the app died in can continue at the next start (#1611). One session per (account, local file); the file path compares ignoring case.</summary>
public interface IUploadSessionStore
{
    /// <summary>Saves the session of <paramref name="source"/>, replacing any earlier one.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="source">The local file.</param>
    /// <param name="session">The session to keep.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, IFileInfo source, UploadSession session, CancellationToken cancellationToken);

    /// <summary>The saved session of <paramref name="source"/>, if there is one.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="source">The local file.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<Option<UploadSession>, ErrorResponse>> GetAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken);

    /// <summary>Forgets the saved session of <paramref name="source"/>. Removing a session that is not saved changes nothing.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="source">The local file.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken);

    /// <summary>Forgets every saved session that has expired at <paramref name="now"/>, for any account (#1641).</summary>
    /// <param name="now">The time to judge expiry against.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>How many sessions were forgotten.</returns>
    Task<Result<int, ErrorResponse>> RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
