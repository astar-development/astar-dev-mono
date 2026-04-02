using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client;

/// <summary>
///     Registers <c>AStar.Dev.OneDrive.Client</c> services with the DI container.
/// </summary>
public static class OneDriveClientServiceExtensions
{
    /// <summary>
    ///     Adds MSAL authentication, token management, consent storage, auth state
    ///     notifications, and OneDrive folder browsing to <paramref name="services" />.
    /// </summary>
    public static IServiceCollection AddOneDriveClient(this IServiceCollection services, OneDriveClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var msalApp = PublicClientApplicationBuilder
            .Create(options.AzureClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "consumers")
            .WithRedirectUri(options.RedirectUri.AbsoluteUri)
            .Build();

        _ = services.AddSingleton<IPublicClientApplication>(msalApp);
        _ = services.AddSingleton<IAuthStateService, AuthStateService>();
        _ = services.AddSingleton<IConsentStore, ConsentStore>();
        _ = services.AddSingleton<IMsalClient, MsalClient>();
        _ = services.AddSingleton<IOneDriveFolderService, OneDriveFolderService>();

        return services;
    }
}
