using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="graphAccess">Gives the account's drive id, access token and Graph client.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports the outcome with the hashed account id, status and Graph error code only.</param>
internal sealed class OneDriveQuotaRefresher(OneDriveGraphAccess graphAccess, TimeProvider timeProvider, ILogger<OneDriveQuotaRefresher> logger) : IQuotaRefresher
{
    /// <inheritdoc />
    public async Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await graphAccess.DriveOfAsync(accountId, cancellationToken)
            .BindAsync(driveId => graphAccess.TokenOfAsync(accountId, outcome => OneDriveQuotaLog.TokenUnavailable(logger, outcome), cancellationToken).BindAsync(token => ReadAsync(accountId, driveId, token, cancellationToken)))
            .ConfigureAwait(false);
    }

    private async Task<Result<StorageUsage, ProviderError>> ReadAsync(AccountId accountId, string driveId, string accessToken, CancellationToken cancellationToken)
    {
        var reading = GetDriveAsync(accountId, driveId, accessToken, cancellationToken);
        await ((Task)reading).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (cancellationToken.IsCancellationRequested)
            cancellationToken.ThrowIfCancellationRequested();

        if (reading.IsCanceled)
            return Faulted(new TaskCanceledException());

        return reading.Exception is { InnerException: { } exception } ? Faulted(exception) : ToUsage(reading.Result);
    }

    private async Task<Drive?> GetDriveAsync(AccountId accountId, string driveId, string accessToken, CancellationToken cancellationToken)
    {
        using var graph = graphAccess.CreateClient(accountId, accessToken);

        return await graph.Drives[driveId].GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Result<StorageUsage, ProviderError> ToUsage(Drive? drive)
    {
        if (drive?.Quota is { Used: { } used, Total: { } total })
            return Result.Success<StorageUsage, ProviderError>(StorageUsageFactory.Create(used, total));

        OneDriveQuotaLog.QuotaMissing(logger);

        return Result.Failure<StorageUsage, ProviderError>(ProviderErrorFactory.CreateUnexpected());
    }

    private Result<StorageUsage, ProviderError> Faulted(Exception exception)
    {
        var (error, statusCode, errorCode) = GraphErrorMapper.Map(exception, timeProvider);
        OneDriveQuotaLog.ReadFailed(logger, statusCode, errorCode, exception.GetType().Name);

        return Result.Failure<StorageUsage, ProviderError>(error);
    }
}
