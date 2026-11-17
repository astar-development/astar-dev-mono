using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reads and writes the links between files and terms.</summary>
public interface IFileClassificationStore
{
    /// <summary>Replaces the file's automatic links with <paramref name="termIds"/>. Links the user assigned stay.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="termIds">The terms that now apply.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken);

    /// <summary>Replaces the automatic links of many files in one transaction, so a batch costs one write.</summary>
    /// <param name="updates">The files and the terms that now apply to each.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken);

    /// <summary>Links a term to a file as the user's own choice. Automatic replacement never removes it.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="termId">The term.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns><see cref="Unit"/> once the link is stored, <see cref="ClassificationFailure.NotFound"/> when the term does not exist, <see cref="ClassificationFailure.FileNotFound"/> when the path is not in the account's current sync baseline, or <see cref="ClassificationFailure.Unavailable"/> when the database fails.</returns>
    Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken);

    /// <summary>Reads the terms linked to a file.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken);

    /// <summary>Reads the files linked to a term.</summary>
    /// <param name="termId">The term.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken);

    /// <summary>Removes every link of the file, for a deleted file.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken);

    /// <summary>Counts the files linked to each term.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken);
}
