using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;

/// <summary>
///     Registers <c>AStar.Dev.OneDrive.Client</c> services with the DI container.
/// </summary>
public static class OneDriveClientServiceExtensions
{
    /// <summary>
    ///     Adds MSAL authentication, token management, consent storage, auth state
    ///     notifications, OneDrive folder browsing, delta queries, and file operations to <paramref name="services" />.
    /// </summary>
    public static IServiceCollection AddOneDriveClient(this IServiceCollection services)
    {
        _ = services.AddSingleton<IPublicClientApplication>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EntraIdConfiguration>>().Value;

            return PublicClientApplicationBuilder
                .Create(options.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, "consumers")
                .WithRedirectUri(options.RedirectUri)
                .Build();
        });

        _ = services.AddHttpClient();

        return services;
    }
}
