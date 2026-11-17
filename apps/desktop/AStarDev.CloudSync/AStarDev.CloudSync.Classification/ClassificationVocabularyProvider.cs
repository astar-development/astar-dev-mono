using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>
/// Loads the terms and categories once and keeps the vocabulary until <see cref="Invalidate"/>. Concurrent callers share one load.
/// A load that is running when <see cref="Invalidate"/> is called is never kept, so a stale vocabulary is never published.
/// A failed or cancelled load is not kept either, so the next call retries.
/// Construct it over the undecorated stores; see <see cref="CachingTermStore"/> for the registration order.
/// </summary>
public sealed class ClassificationVocabularyProvider(ITermStore termStore, ICategoryStore categoryStore) : IClassificationVocabularyProvider
{
    private long generation;
    private readonly Lock gate = new();
    private Option<Task<Result<ClassificationVocabulary, ClassificationFailure>>> current = Option.None<Task<Result<ClassificationVocabulary, ClassificationFailure>>>();

    /// <inheritdoc />
    public long Generation => Interlocked.Read(ref generation);

    /// <inheritdoc />
    public async Task<Result<ClassificationVocabulary, ClassificationFailure>> GetAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var load = JoinOrStart(cancellationToken);
            await ((Task)load).WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            cancellationToken.ThrowIfCancellationRequested();

            if (!load.IsCanceled)
            {
                return await load.ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        lock (gate)
        {
            _ = Interlocked.Increment(ref generation);
            current = Option.None<Task<Result<ClassificationVocabulary, ClassificationFailure>>>();
        }
    }

    private Task<Result<ClassificationVocabulary, ClassificationFailure>> JoinOrStart(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return current.Match(task => task.IsCanceled || task.IsFaulted ? Start(cancellationToken) : task, () => Start(cancellationToken));
        }
    }

    private Task<Result<ClassificationVocabulary, ClassificationFailure>> Start(CancellationToken cancellationToken)
    {
        var load = Task.Run(() => LoadAsync(cancellationToken), cancellationToken);
        current = Option.Some(load);
        _ = load.ContinueWith(finished => Forget(finished), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return load;
    }

    private void Forget(Task<Result<ClassificationVocabulary, ClassificationFailure>> finished)
    {
        if (finished.IsCompletedSuccessfully && finished.Result.Match(_ => true, _ => false))
        {
            return;
        }

        lock (gate)
        {
            current = current.Match(task => ReferenceEquals(task, finished) ? Option.None<Task<Result<ClassificationVocabulary, ClassificationFailure>>>() : current, () => current);
        }
    }

    private Task<Result<ClassificationVocabulary, ClassificationFailure>> LoadAsync(CancellationToken cancellationToken) =>
        termStore.GetAllAsync(cancellationToken).BindAsync(terms => categoryStore.GetAllAsync(cancellationToken).MatchAsync(
            categories => (Result<ClassificationVocabulary, ClassificationFailure>)ClassificationVocabularyFactory.Create(categories, terms),
            failure => (Result<ClassificationVocabulary, ClassificationFailure>)failure));
}
