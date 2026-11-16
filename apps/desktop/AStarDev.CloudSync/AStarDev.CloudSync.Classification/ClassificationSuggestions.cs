using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Suggests terms from the words in file paths.</summary>
/// <param name="fileSource">The streamed file paths.</param>
/// <param name="termStore">The term store.</param>
/// <param name="knownWords">The common-word list.</param>
/// <param name="firstNames">The first-name list.</param>
public sealed class ClassificationSuggestions(IClassifiableFileSource fileSource, ITermStore termStore, IKnownWordList knownWords, IFirstNameList firstNames) : IClassificationSuggestions
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>> GetSuggestionsAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{fileSource}{termStore}{knownWords}{firstNames}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> PromoteAsync(string word, CategoryId categoryId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{word}{categoryId}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> IgnoreAsync(string word, CategoryId categoryId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{word}{categoryId}{cancellationToken}");
}
