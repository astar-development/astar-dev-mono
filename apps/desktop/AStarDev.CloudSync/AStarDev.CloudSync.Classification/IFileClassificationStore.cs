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

    /// <summary>Removes every link of the file, for a deleted file.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken);

    /// <summary>Counts the files linked to each term.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken);
}
