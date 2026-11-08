using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class OneDriveModule
{
    private const string CloudSyncCategoryPrefix = "AStarDev.CloudSync.";

    private readonly ServiceProvider provider;

    public OneDriveModule(OneDriveFakes fakes)
    {
        ArgumentNullException.ThrowIfNull(fakes);

        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(fakes.Logs).AddFilter((category, _) => category?.StartsWith(CloudSyncCategoryPrefix, StringComparison.Ordinal) == true))
            .AddOneDriveAccounts(new ConfigurationBuilder().Build(), _ => fakes.ConsentFile.Folder, _ => fakes.ConsentFile.File)
            .AddSingleton<IMsalTokenCacheStorage>(fakes.Storage)
            .AddSingleton<IMsalClient>(fakes.Msal)
            .AddSingleton<IHttpClientFactory>(fakes.HttpClientFactory)
            .AddSingleton<ITokenCacheConsent>(fakes.Consent)
            .AddSingleton<IAccountStore>(fakes.AccountStore)
            .AddSingleton(fakes.Time)
            .BuildServiceProvider();
    }

    public IAccountAdder Adder => provider.GetRequiredService<IAccountAdder>();

    public IPlaintextCacheConsentSettings ConsentSettings => provider.GetRequiredService<IPlaintextCacheConsentSettings>();

    public IAccessTokenSource TokenSource => provider.GetRequiredService<IAccessTokenSource>();

    public Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken) => Adder.AddAsync(cancellationToken);

    public async Task<Result<Unit, AccountAddFailure>> EnsureTokenCacheAsync(CancellationToken cancellationToken) =>
        await AddAsync(cancellationToken).ConfigureAwait(false) switch
        {
            AccountAddOutcome.Failed failed => Result.Failure<Unit, AccountAddFailure>(failed.Failure),
            AccountAddOutcome.Cancelled => throw new OperationCanceledException(cancellationToken),
            _ => Result.Success<Unit, AccountAddFailure>(Unit.Instance),
        };

    public PlaintextCacheConsent Current() => ConsentSettings.Current();

    public Task<Result<Unit, ErrorResponse>> WithdrawAsync() => ConsentSettings.WithdrawAsync();
}
