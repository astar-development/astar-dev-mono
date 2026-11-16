using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Finds words in file paths that are not yet terms, and lets the user promote or ignore them. Nothing is ever assigned automatically.</summary>
public interface IClassificationSuggestions
{
    /// <summary>Scans the file paths and returns unreviewed words ranked by file count descending, then alphabetically.</summary>
    /// <param name="cancellationToken">Cancels the scan.</param>
    Task<Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>> GetSuggestionsAsync(CancellationToken cancellationToken);

    /// <summary>Makes the word a user term in the category, or promotes the existing suggested or ignored term. Repeated and parallel calls give one term.</summary>
    /// <param name="word">The suggested word.</param>
    /// <param name="categoryId">The category to assign.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<ClassificationTerm, ClassificationFailure>> PromoteAsync(string word, CategoryId categoryId, CancellationToken cancellationToken);

    /// <summary>Marks the word ignored so it never comes back as a suggestion. The term is created if needed and never deleted.</summary>
    /// <param name="word">The suggested word.</param>
    /// <param name="categoryId">The category used when the term has to be created.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<ClassificationTerm, ClassificationFailure>> IgnoreAsync(string word, CategoryId categoryId, CancellationToken cancellationToken);
}
