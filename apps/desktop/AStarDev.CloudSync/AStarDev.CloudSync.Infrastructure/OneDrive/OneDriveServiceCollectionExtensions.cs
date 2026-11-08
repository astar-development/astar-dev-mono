using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Registers the personal Microsoft account sign-in, token cache and OneDrive drive lookup.</summary>
public static class OneDriveServiceCollectionExtensions
{
    /// <summary>Registers one MSAL public client application, the token cache storage, the Graph drive lookup, the <see cref="IAccountAdder"/> and the <see cref="IPlaintextCacheConsentSettings"/>. The app must also register an <see cref="ITokenCacheConsent"/>.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The configuration holding the <see cref="MicrosoftAccountOptions.SectionName"/> section.</param>
    /// <param name="tokenCacheFolder">Gives the folder that holds the token cache files.</param>
    /// <param name="consentFile">Gives the file that remembers the user's consent to a plaintext token file.</param>
    /// <returns>The <paramref name="services"/> collection to allow further chaining.</returns>
    public static IServiceCollection AddOneDriveAccounts(this IServiceCollection services, IConfiguration configuration, Func<IServiceProvider, IDirectoryInfo> tokenCacheFolder, Func<IServiceProvider, IFileInfo> consentFile)
    {
        services.TryAddSingleton(TimeProvider.System);
        _ = services.AddHttpClient(GraphDriveLookup.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => GraphClientFactory.CreatePipeline(GraphClientFactory.CreateDefaultHandlers()));

        return services
            .Configure<MicrosoftAccountOptions>(configuration.GetSection(MicrosoftAccountOptions.SectionName))
            .AddSingleton<MsalIdentityLogger>()
            .AddSingleton<IPublicClientApplication>(CreatePublicClientApplication)
            .AddSingleton<IMsalClient, MsalClient>()
            .AddSingleton<IMsalTokenCacheStorage>(provider => new MsalCacheHelperStorage(provider.GetRequiredService<IPublicClientApplication>(), tokenCacheFolder(provider)))
            .AddSingleton<IPlaintextCacheConsentStore>(provider => new JsonPlaintextCacheConsentStore(consentFile(provider), provider.GetRequiredService<ILogger<JsonPlaintextCacheConsentStore>>()))
            .AddSingleton<TokenCacheInitialiser>()
            .AddSingleton<IPlaintextCacheConsentSettings>(provider => provider.GetRequiredService<TokenCacheInitialiser>())
            .AddSingleton<IDriveLookup, GraphDriveLookup>()
            .AddSingleton<IAccountAdder, MicrosoftAccountAdder>();
    }

    private static IPublicClientApplication CreatePublicClientApplication(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<MicrosoftAccountOptions>>().Value;

        return PublicClientApplicationBuilder.Create(options.ClientId)
            .WithAuthority(options.Authority)
            .WithRedirectUri(options.RedirectUri.OriginalString)
            .WithLogging(provider.GetRequiredService<MsalIdentityLogger>(), enablePiiLogging: false)
            .Build();
    }
}
