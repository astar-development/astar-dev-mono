using System.IO.Abstractions;
using AStarDev.CloudSync.Core.Accounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Registers the personal Microsoft account sign-in, token cache and OneDrive drive lookup.</summary>
public static class OneDriveServiceCollectionExtensions
{
    /// <summary>Registers one MSAL public client application, the token cache storage, the Graph drive lookup and the <see cref="IAccountAdder"/>. The app must also register an <see cref="ITokenCacheConsent"/>.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The configuration holding the <see cref="MicrosoftAccountOptions.SectionName"/> section.</param>
    /// <param name="tokenCacheFolder">Gives the folder that holds the token cache files.</param>
    /// <returns>The <paramref name="services"/> collection to allow further chaining.</returns>
    public static IServiceCollection AddOneDriveAccounts(this IServiceCollection services, IConfiguration configuration, Func<IServiceProvider, IDirectoryInfo> tokenCacheFolder)
    {
        _ = services.AddHttpClient(GraphDriveLookup.HttpClientName);

        return services
            .Configure<MicrosoftAccountOptions>(configuration.GetSection(MicrosoftAccountOptions.SectionName))
            .AddSingleton<IPublicClientApplication>(CreatePublicClientApplication)
            .AddSingleton<IMsalClient, MsalClient>()
            .AddSingleton<IMsalTokenCacheStorage>(provider => new MsalCacheHelperStorage(provider.GetRequiredService<IPublicClientApplication>(), tokenCacheFolder(provider)))
            .AddSingleton<TokenCacheInitialiser>()
            .AddSingleton<IDriveLookup, GraphDriveLookup>()
            .AddSingleton<IAccountAdder, MicrosoftAccountAdder>();
    }

    private static IPublicClientApplication CreatePublicClientApplication(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<MicrosoftAccountOptions>>().Value;

        return PublicClientApplicationBuilder.Create(options.ClientId)
            .WithAuthority(options.Authority)
            .WithRedirectUri(options.RedirectUri.OriginalString)
            .Build();
    }
}
