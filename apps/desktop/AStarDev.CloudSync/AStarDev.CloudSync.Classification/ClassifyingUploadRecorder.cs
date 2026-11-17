using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Decorates the upload recorder so each uploaded file is classified after the baseline row is recorded. The inner result is always returned unchanged; a classification failure is logged and never reaches the sync. Register it by resolving the inner recorder by its undecorated key, so the decorator never resolves <see cref="IUploadRecorder"/> itself and no cycle forms.</summary>
/// <param name="inner">The registered recorder that stores the baseline and the remote index row.</param>
/// <param name="classifier">Classifies one path.</param>
/// <param name="store">Stores the automatic links.</param>
/// <param name="vocabulary">Reports whether the vocabulary changed while the file was classified.</param>
/// <param name="logger">Reports failures without paths, terms or exception messages.</param>
public sealed class ClassifyingUploadRecorder(IUploadRecorder inner, IFileClassifier classifier, IFileClassificationStore store, IClassificationVocabularyProvider vocabulary, ILogger<ClassifyingUploadRecorder> logger) : IUploadRecorder
{
    private const int MaxAttempts = 3;

    /// <inheritdoc />
    public async Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken)
    {
        var result = await inner.RecordAsync(accountId, baseline, remote, cancellationToken).ConfigureAwait(false);
        if (!result.Match(_ => true, _ => false) || baseline.Kind is not SyncBaselineKind.File)
            return result;

        await Try.RunAsync(() => ClassifyAsync(accountId, baseline.Path, cancellationToken), cancellationToken).MatchAsync(_ => Unit.Instance, exception => Failed(exception)).ConfigureAwait(false);

        return result;
    }

    private async Task<Unit> ClassifyAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt < MaxAttempts; attempt++)
        {
            var generation = vocabulary.Generation;
            _ = await ClassifyOnceAsync(accountId, path, cancellationToken).ConfigureAwait(false);
            if (vocabulary.Generation == generation)
                return Unit.Instance;
        }

        return await ClassifyOnceAsync(accountId, path, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> ClassifyOnceAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        var classified = await classifier.ClassifyAsync(path, cancellationToken).ConfigureAwait(false);
        var links = FileTermLinksFactory.Create(accountId, path, [.. classified.Candidates.Select(candidate => candidate.Term.Id)]);

        return await store.ReplaceAutoLinksAsync([links], cancellationToken).MatchAsync(_ => Unit.Instance, failure => Failed(failure)).ConfigureAwait(false);
    }

    private Unit Failed(Exception exception)
    {
        LogMessage.Warning(logger, nameof(ClassifyingUploadRecorder), "An uploaded file could not be classified after the baseline write", exception.ErrorType());

        return Unit.Instance;
    }

    private Unit Failed(ClassificationFailure failure)
    {
        LogMessage.Warning(logger, nameof(ClassifyingUploadRecorder), "The classification links were not updated after the upload was recorded", failure.GetType().Name);

        return Unit.Instance;
    }
}
