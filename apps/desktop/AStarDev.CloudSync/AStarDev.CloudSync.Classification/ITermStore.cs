using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reads and writes classification terms.</summary>
public interface ITermStore
{
    /// <summary>Reads every term, ignored ones included.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Adds the term, or returns the stored one when it exists. A term is the same when its text and category match, so one word can live in two categories. An existing ignored term stays ignored and is never recreated.</summary>
    /// <param name="term">The term to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken);

    /// <summary>Adds the terms, or returns the stored ones, with the same rules as <see cref="UpsertAsync" /> for each term. The stored terms come back in the order of <paramref name="terms" />; terms that share a category and text give the same stored term. An implementation writes in bounded transactions, so a very large list is written in several. When any term fails, the result is the failure.</summary>
    /// <param name="terms">The terms to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken);

    /// <summary>Reads one term by id.</summary>
    /// <param name="termId">The term.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken);

    /// <summary>Marks the term active or ignored. The term is never deleted.</summary>
    /// <param name="termId">The term.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken);
}
