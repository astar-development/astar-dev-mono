using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Decorates the sync baseline writer so each file written is classified and each file removed has its classification links removed. The inner result is always returned unchanged; a classification failure is logged and never reaches the sync. Register it with <see cref="ClassificationServiceCollectionExtensions.AddClassification"/>, which resolves the inner writer by its undecorated key, so the decorator never resolves <see cref="ISyncBaselineWriter"/> itself and no cycle forms.</summary>
/// <param name="inner">The registered writer that stores the baseline.</param>
/// <param name="classifier">Classifies one path.</param>
/// <param name="store">Stores the automatic links.</param>
/// <param name="vocabulary">Reports whether the vocabulary changed while a batch was classified.</param>
/// <param name="logger">Reports failures without paths, terms or exception messages.</param>
public sealed class ClassifyingSyncBaselineWriter(ISyncBaselineWriter inner, IFileClassifier classifier, IFileClassificationStore store, IClassificationVocabularyProvider vocabulary, ILogger<ClassifyingSyncBaselineWriter> logger) : ISyncBaselineWriter
{
    private const int MaxAttempts = 3;

    /// <inheritdoc />
    public async Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken)
    {
        var result = await inner.UpsertAsync(accountId, entries, cancellationToken).ConfigureAwait(false);
        if (!result.Match(_ => true, _ => false))
            return result;

        await Try.RunAsync(() => ClassifyAsync(accountId, entries, cancellationToken), cancellationToken).MatchAsync(_ => Unit.Instance, exception => Failed(exception)).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken)
    {
        var result = await inner.RemoveAsync(accountId, paths, cancellationToken).ConfigureAwait(false);
        if (!result.Match(_ => true, _ => false))
            return result;

        foreach (var path in paths)
            await Try.RunAsync(() => UnlinkAsync(accountId, path, cancellationToken), cancellationToken).MatchAsync(_ => Unit.Instance, exception => Failed(exception)).ConfigureAwait(false);

        return result;
    }

    private async Task<Unit> ClassifyAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt < MaxAttempts; attempt++)
        {
            var generation = vocabulary.Generation;
            _ = await ClassifyOnceAsync(accountId, entries, cancellationToken).ConfigureAwait(false);
            if (vocabulary.Generation == generation)
                return Unit.Instance;
        }

        return await ClassifyOnceAsync(accountId, entries, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> ClassifyOnceAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken)
    {
        var links = new List<FileTermLinks>();
        foreach (var path in LastFilePaths(entries))
        {
            var classified = await classifier.ClassifyAsync(path, cancellationToken).ConfigureAwait(false);
            links.Add(FileTermLinksFactory.Create(accountId, path, [.. classified.Candidates.Select(candidate => candidate.Term.Id)]));
        }

        if (links.Count == 0)
            return Unit.Instance;

        return await store.ReplaceAutoLinksAsync(links, cancellationToken).MatchAsync(_ => Unit.Instance, failure => Failed(failure)).ConfigureAwait(false);
    }

    private async Task<Unit> UnlinkAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) =>
        await store.RemoveAsync(accountId, path, cancellationToken).MatchAsync(_ => Unit.Instance, failure => Failed(failure)).ConfigureAwait(false);

    private static List<RelativeSyncPath> LastFilePaths(IReadOnlyList<SyncBaselineEntry> entries)
    {
        var seen = new HashSet<RelativeSyncPath>();
        var paths = new List<RelativeSyncPath>();
        for (var index = entries.Count - 1; index >= 0; index--)
            if (entries[index].Kind is SyncBaselineKind.File && seen.Add(entries[index].Path))
                paths.Add(entries[index].Path);

        paths.Reverse();

        return paths;
    }

    private Unit Failed(Exception exception)
    {
        LogMessage.Warning(logger, nameof(ClassifyingSyncBaselineWriter), "A file could not be classified after the baseline write", exception.ErrorType());

        return Unit.Instance;
    }

    private Unit Failed(ClassificationFailure failure)
    {
        LogMessage.Warning(logger, nameof(ClassifyingSyncBaselineWriter), "The classification links were not updated after the baseline write", failure.GetType().Name);

        return Unit.Instance;
    }
}
