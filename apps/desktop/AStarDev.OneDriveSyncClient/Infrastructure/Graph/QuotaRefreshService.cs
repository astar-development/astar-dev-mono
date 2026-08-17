using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <inheritdoc />
public sealed class QuotaRefreshService(IGraphService graphService, IAuthService authService, IAccountRepository accountRepository, ILogger<QuotaRefreshService> logger) : IQuotaRefreshService
{
    /// <inheritdoc />
    public async Task TryRefreshAsync(OneDriveAccount account, CancellationToken cancellationToken = default)
    {
        var tokenResult = await authService.AcquireTokenSilentAsync(account.Id.Value, cancellationToken).ConfigureAwait(false);

        await tokenResult
            .TapError(_ => OneDriveSyncClientMessages.QuotaRefreshTokenFailed(logger, account.Id.Value))
            .TapAsync(auth => ApplyQuotaAsync(account, auth, cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task ApplyQuotaAsync(OneDriveAccount account, AuthResult auth, CancellationToken cancellationToken)
    {
        var quotaResult = await graphService.GetQuotaAsync(account.Id.Value, _ => Task.FromResult(auth.AccessToken), cancellationToken).ConfigureAwait(false);

        await quotaResult
            .TapError(error => OneDriveSyncClientMessages.QuotaRefreshFetchFailed(logger, account.Id.Value, error))
            .TapAsync(async quota =>
            {
                account.Quota = StorageQuotaFactory.Create(quota.Total, quota.Used);
                await accountRepository.UpdateQuotaAsync(account.Id, account.Quota, cancellationToken).ConfigureAwait(false);
            })
            .ConfigureAwait(false);
    }
}
