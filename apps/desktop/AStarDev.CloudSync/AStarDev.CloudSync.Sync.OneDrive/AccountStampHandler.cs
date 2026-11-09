using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal sealed class AccountStampHandler(AccountId accountId, HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Options.Set(OneDriveSyncHttp.AccountOption, accountId);

        return base.SendAsync(request, cancellationToken);
    }
}
