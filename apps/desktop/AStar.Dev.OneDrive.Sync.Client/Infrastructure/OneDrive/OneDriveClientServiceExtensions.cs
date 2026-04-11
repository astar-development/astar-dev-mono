using System.IO.Abstractions;
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
    public static IServiceCollection AddOneDriveClient(this IServiceCollection services, IOptions<EntraIdConfiguration> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var msalApp = PublicClientApplicationBuilder
            .Create(options.Value.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "consumers")
            .WithRedirectUri(options.Value.RedirectUri)
            .Build();

        _ = services.AddSingleton<IPublicClientApplication>(msalApp);
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddHttpClient();

        return services;
    }
}
