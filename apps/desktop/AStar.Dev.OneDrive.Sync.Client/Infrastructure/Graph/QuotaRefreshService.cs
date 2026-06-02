using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <inheritdoc />
public sealed class QuotaRefreshService(IGraphService graphService, IAuthService authService, IAccountRepository accountRepository, ILogger<QuotaRefreshService> logger) : IQuotaRefreshService
{
    /// <inheritdoc />
    public async Task TryRefreshAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        var tokenResult = await authService.AcquireTokenSilentAsync(account.Id.Id, ct).ConfigureAwait(false);

        await tokenResult
            .TapError(_ => OneDriveSyncClientMessages.QuotaRefreshTokenFailed(logger, account.Id.Id))
            .TapAsync(auth => ApplyQuotaAsync(account, auth, ct))
            .ConfigureAwait(false);
    }

    private async Task ApplyQuotaAsync(OneDriveAccount account, AuthResult auth, CancellationToken ct)
    {
        var quotaResult = await graphService.GetQuotaAsync(account.Id.Id, _ => Task.FromResult(auth.AccessToken), ct).ConfigureAwait(false);

        await quotaResult
            .TapError(error => OneDriveSyncClientMessages.QuotaRefreshFetchFailed(logger, account.Id.Id, error))
            .TapAsync(async quota =>
            {
                account.Quota = StorageQuotaFactory.Create(quota.Total, quota.Used);
                await accountRepository.UpdateQuotaAsync(account.Id, account.Quota, ct).ConfigureAwait(false);
            })
            .ConfigureAwait(false);
    }
}
