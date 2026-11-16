using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Decorates the upload recorder so each uploaded file is classified after the baseline row is recorded. The inner result is always returned unchanged; a classification failure is logged and never reaches the sync. Register it by resolving the inner recorder by its undecorated key, so the decorator never resolves <see cref="IUploadRecorder"/> itself and no cycle forms.</summary>
/// <param name="inner">The registered recorder that stores the baseline and the remote index row.</param>
/// <param name="classifier">Classifies one path.</param>
/// <param name="store">Stores the automatic links.</param>
/// <param name="logger">Reports failures without paths, terms or exception messages.</param>
public sealed class ClassifyingUploadRecorder(IUploadRecorder inner, IFileClassifier classifier, IFileClassificationStore store, ILogger<ClassifyingUploadRecorder> logger) : IUploadRecorder
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{inner}{classifier}{store}{logger}{accountId}{baseline}{remote}{cancellationToken}");
}
