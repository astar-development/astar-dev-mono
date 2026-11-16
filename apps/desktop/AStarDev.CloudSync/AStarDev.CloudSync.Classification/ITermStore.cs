using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reads and writes classification terms.</summary>
public interface ITermStore
{
    /// <summary>Reads every term, ignored ones included.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Adds the term, or returns the stored one when it exists. An existing ignored term stays ignored and is never recreated.</summary>
    /// <param name="term">The term to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken);

    /// <summary>Marks the term active or ignored. The term is never deleted.</summary>
    /// <param name="termId">The term.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken);
}
