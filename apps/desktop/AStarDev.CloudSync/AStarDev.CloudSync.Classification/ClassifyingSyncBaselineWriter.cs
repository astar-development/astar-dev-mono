using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Decorates the sync baseline writer so each file written is classified and each file removed has its classification links removed. The inner result is always returned unchanged; a classification failure is logged and never reaches the sync. Register it in the composition root (#1822) by resolving the inner writer by its concrete type, for example <c>services.AddSingleton&lt;ISyncBaselineWriter&gt;(provider =&gt; new ClassifyingSyncBaselineWriter(provider.GetRequiredService&lt;SqliteSyncBaselineStore&gt;(), ...))</c>, so the decorator never resolves <see cref="ISyncBaselineWriter"/> itself and no cycle forms.</summary>
/// <param name="inner">The registered writer that stores the baseline.</param>
/// <param name="classifier">Classifies one path.</param>
/// <param name="store">Stores the automatic links.</param>
/// <param name="logger">Reports failures without paths, terms or exception messages.</param>
public sealed class ClassifyingSyncBaselineWriter(ISyncBaselineWriter inner, IFileClassifier classifier, IFileClassificationStore store, ILogger<ClassifyingSyncBaselineWriter> logger) : ISyncBaselineWriter
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{entries}{cancellationToken}{inner}{classifier}{store}{logger}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{paths}{cancellationToken}");
}
