using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Runs the steps of an upload plan.</summary>
/// <param name="recorder">Writes the baseline and remote index rows of an uploaded file in one transaction.</param>
/// <param name="baseline">Writes folder and deletion changes to the baseline.</param>
/// <param name="options">The concurrency limit.</param>
internal sealed class UploadItemExecutor(IUploadRecorder recorder, ISyncBaselineWriter baseline, UploadPassOptions options)
{
    internal Task<Result<IReadOnlyList<UploadItemOutcome>, UploadPassFailure>> ExecuteAsync(AccountId accountId, UploadPlan plan, UploadPassInput input, UploadProviders providers, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{recorder}{baseline}{options}{accountId}{plan}{input}{providers}{cancellationToken}");
}
