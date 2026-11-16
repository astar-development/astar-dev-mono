using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Suggests terms from the words in file paths.</summary>
/// <param name="fileSource">The streamed file paths.</param>
/// <param name="termStore">The term store.</param>
/// <param name="knownWords">The common-word list.</param>
/// <param name="firstNames">The first-name list.</param>
public sealed class ClassificationSuggestions(IClassifiableFileSource fileSource, ITermStore termStore, IKnownWordList knownWords, IFirstNameList firstNames) : IClassificationSuggestions, IDisposable
{
    private readonly SemaphoreSlim writeGate = new(1, 1);

    /// <inheritdoc />
    public void Dispose() => writeGate.Dispose();

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>> GetSuggestionsAsync(CancellationToken cancellationToken) =>
        termStore.GetAllAsync(cancellationToken).BindAsync(terms => RankAsync(terms, cancellationToken));

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> PromoteAsync(string word, CategoryId categoryId, CancellationToken cancellationToken) =>
        WriteAsync(word, categoryId, Promote, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> IgnoreAsync(string word, CategoryId categoryId, CancellationToken cancellationToken) =>
        WriteAsync(word, categoryId, Ignore, cancellationToken);

    private static ClassificationTerm? Promote(ClassificationTerm? existing, ClassificationTerm candidate) => existing switch
    {
        { Status: TermStatus.Active, Source: not TermSource.Suggested } => null,
        { } found => Rebuild(found, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive()),
        _ => Rebuild(candidate, TermSourceFactory.CreateUser(), TermStatusFactory.CreateActive())
    };

    private static ClassificationTerm? Ignore(ClassificationTerm? existing, ClassificationTerm candidate) => existing switch
    {
        { Status: TermStatus.Ignored } => null,
        { } found => Rebuild(found, found.Source, TermStatusFactory.CreateIgnored()),
        _ => Rebuild(candidate, TermSourceFactory.CreateSuggested(), TermStatusFactory.CreateIgnored())
    };

    private static ClassificationTerm Rebuild(ClassificationTerm term, TermSource source, TermStatus status) =>
        ClassificationTermFactory.Create(term.Id, term.Text, term.CategoryId, source, status).Match(rebuilt => rebuilt, () => term);

    private static Result<ClassificationTerm, ClassificationFailure> Unchanged(ClassificationTerm term) => Result.Success<ClassificationTerm, ClassificationFailure>(term);

    private async Task<Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>> RankAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken)
    {
        var knownTokens = terms.Select(term => ClassificationTokeniser.Tokenise(term.Text)).Where(tokens => tokens.Count == 1).Select(tokens => tokens[0]).ToHashSet(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var scan = await Try.RunAsync(() => CountAsync(counts, knownTokens, cancellationToken), cancellationToken).ConfigureAwait(false);

        return scan.Match(_ => Result.Success<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>(Rank(counts)), _ => Result.Failure<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));
    }

    private async Task<Unit> CountAsync(Dictionary<string, int> counts, HashSet<string> knownTokens, CancellationToken cancellationToken)
    {
        await foreach (var file in fileSource.StreamAsync(cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var word in SuggestionWords.Split(file.Path.Value).Where(word => !IsKnown(word, knownTokens)))
            {
                counts[word] = counts.GetValueOrDefault(word) + 1;
            }
        }

        return Unit.Instance;
    }

    private bool IsKnown(string word, HashSet<string> knownTokens) => knownTokens.Contains(ClassificationTokeniser.Tokenise(word)[0]) || knownWords.Contains(word);

    private IReadOnlyList<ClassificationSuggestion> Rank(Dictionary<string, int> counts) =>
        [.. counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => ClassificationSuggestionFactory.Create(pair.Key, pair.Value, firstNames.Contains(pair.Key) ? SuggestionHintFactory.CreateFirstName() : SuggestionHintFactory.CreatePlain()))];

    private async Task<Result<ClassificationTerm, ClassificationFailure>> WriteAsync(string word, CategoryId categoryId, Func<ClassificationTerm?, ClassificationTerm, ClassificationTerm?> decide, CancellationToken cancellationToken)
    {
        var candidate = ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), word, categoryId, TermSourceFactory.CreateSuggested(), TermStatusFactory.CreateActive());

        return await candidate.Match(
            term => LockedWriteAsync(term, decide, cancellationToken),
            () => Task.FromResult(Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound()))).ConfigureAwait(false);
    }

    private async Task<Result<ClassificationTerm, ClassificationFailure>> LockedWriteAsync(ClassificationTerm candidate, Func<ClassificationTerm?, ClassificationTerm, ClassificationTerm?> decide, CancellationToken cancellationToken)
    {
        await writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        using var release = new GateRelease(writeGate);

        return await termStore.GetAllAsync(cancellationToken).BindAsync(terms => SaveAsync(terms.FirstOrDefault(term => term.CategoryId == candidate.CategoryId && term.Text == candidate.Text), candidate, decide, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<ClassificationTerm, ClassificationFailure>> SaveAsync(ClassificationTerm? existing, ClassificationTerm candidate, Func<ClassificationTerm?, ClassificationTerm, ClassificationTerm?> decide, CancellationToken cancellationToken) =>
        decide(existing, candidate) is { } changed ? await termStore.UpsertAsync(changed, cancellationToken).ConfigureAwait(false) : Unchanged(existing!);

    private sealed class GateRelease(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
