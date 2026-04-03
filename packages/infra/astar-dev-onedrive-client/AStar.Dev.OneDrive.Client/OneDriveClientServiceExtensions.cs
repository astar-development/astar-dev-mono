using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDrive.Client.Features.DeltaQueries;
using AStar.Dev.OneDrive.Client.Features.FileOperations;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;
using AStar.Dev.OneDrive.Client.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System.IO.Abstractions;

namespace AStar.Dev.OneDrive.Client;

/// <summary>
///     Registers <c>AStar.Dev.OneDrive.Client</c> services with the DI container.
/// </summary>
public static class OneDriveClientServiceExtensions
{
    /// <summary>
    ///     Adds MSAL authentication, token management, consent storage, auth state
    ///     notifications, OneDrive folder browsing, delta queries, and file operations to <paramref name="services" />.
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
        _ = services.AddSingleton<IGraphClientFactory, GraphClientFactory>();
        _ = services.AddSingleton<IDeltaQueryService, DeltaQueryService>();
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddHttpClient();
        _ = services.AddSingleton<IFileDownloader, FileDownloader>();
        _ = services.AddSingleton<IFileUploader, FileUploader>();

        return services;
    }
}
