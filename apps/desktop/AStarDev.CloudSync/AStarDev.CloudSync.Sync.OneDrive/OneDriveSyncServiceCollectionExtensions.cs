using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Registers the OneDrive sync module.</summary>
public static class OneDriveSyncServiceCollectionExtensions
{
    /// <summary>Registers the OneDrive sync adapters and the <see cref="OneDriveSyncHttp.ClientName"/> HTTP client with the account throttle gate. Needs <see cref="SyncServiceCollectionExtensions.AddCloudSync"/>.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddOneDriveSync(this IServiceCollection services)
    {
        services.TryAddSingleton(new RetryHandlerOption());
        services.TryAddSingleton<OneDriveGraphAccess>();
        _ = services.AddTransient<AccountThrottleHandler>()
            .AddHttpClient(OneDriveSyncHttp.ClientName)
            .RemoveAllLoggers()
            .AddHttpMessageHandler<AccountThrottleHandler>();

        return services.AddKeyedSingleton<IRemoteFolderBrowser, OneDriveFolderBrowser>(CloudProviderFactory.CreateOneDrive())
            .AddKeyedSingleton<IRemoteChangeFeed, OneDriveChangeFeed>(CloudProviderFactory.CreateOneDrive());
    }
}
