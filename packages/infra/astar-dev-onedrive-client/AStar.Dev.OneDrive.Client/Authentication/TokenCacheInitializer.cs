using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Registers the MSAL token cache on an <see cref="IPublicClientApplication"/>.
///     On platforms where the OS keychain is available the cache is secured by
///     the keychain.  When the keychain is unavailable (common on headless Linux),
///     explicit user consent is required before falling back to a machine-scoped
///     encrypted local file — tokens are never stored as plain text (AU-02, AU-03).
/// </summary>
public sealed class TokenCacheInitializer
{
    private const string ConsentMessage =
        "The secure OS keychain is unavailable on this machine. " +
        "To avoid signing in every time the application starts, your authentication token " +
        "can be stored in an encrypted local file. " +
        "The token is protected with a machine-scoped key and is never stored as plain text. " +
        "Would you like to enable this?";

    private readonly IConsentStore _consentStore;
    private readonly IConsentPrompt _consentPrompt;
    private readonly Func<StorageCreationProperties, ITokenCache, Task> _registerKeychainCache;

    /// <summary>
    ///     Production constructor: uses <see cref="MsalCacheHelper"/> to register the
    ///     OS-keychain-backed cache.
    /// </summary>
    public TokenCacheInitializer(IConsentStore consentStore, IConsentPrompt consentPrompt)
        : this(consentStore, consentPrompt, RegisterViaHelperAsync)
    {
    }

    /// <summary>
    ///     Overload that accepts an injectable keychain-registration delegate,
    ///     enabling the keychain path to be replaced in unit tests.
    /// </summary>
    internal TokenCacheInitializer(
        IConsentStore                                      consentStore,
        IConsentPrompt                                     consentPrompt,
        Func<StorageCreationProperties, ITokenCache, Task> registerKeychainCache)
    {
        _consentStore          = consentStore;
        _consentPrompt         = consentPrompt;
        _registerKeychainCache = registerKeychainCache;
    }

    /// <summary>
    ///     Registers the token cache on <paramref name="app"/>.
    ///     Tries the OS keychain first; on failure, gates the insecure fallback
    ///     behind explicit per-account user consent (AU-03).
    /// </summary>
    /// <param name="app">The MSAL public client application to configure.</param>
    /// <param name="options">Authentication options supplying cache path settings.</param>
    /// <param name="accountId">
    ///     The unique identifier of the account whose consent record is consulted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the OS keychain is unavailable and the user declines
    ///     consent for the insecure fallback store.
    /// </exception>
    public async Task InitializeAsync(
        IPublicClientApplication app,
        AuthenticationOptions    options,
        string                   accountId,
        CancellationToken        cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        Directory.CreateDirectory(options.TokenCacheDirectory);

        // Build keyring-only storage properties. WithLinuxUnprotectedFile() is intentionally
        // omitted here: in MSAL Extensions 4.65+ plaintext storage is mutually exclusive with
        // keyring storage. The insecure fallback is handled separately below (AU-03).
        var storageProperties = new StorageCreationPropertiesBuilder(
                options.TokenCacheFileName,
                options.TokenCacheDirectory)
            .WithLinuxKeyring(
                schemaName:  "com.astardev.onedrivesync",
                collection:  MsalCacheHelper.LinuxKeyRingDefaultCollection,
                secretLabel: "AStar Dev OneDrive Sync token cache",
                attribute1:  new KeyValuePair<string, string>("Version", "1"),
                attribute2:  new KeyValuePair<string, string>("ProductGroup", "AStar.Dev"))
            .Build();

        try
        {
            await _registerKeychainCache(storageProperties, app.UserTokenCache).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // MsalCacheHelper can throw various platform-specific exceptions when the keychain is unavailable; we intentionally fall back for any failure.
        catch(Exception)
#pragma warning restore CA1031
        {
            await RegisterInsecureCacheAsync(app, options, accountId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RegisterInsecureCacheAsync(
        IPublicClientApplication app,
        AuthenticationOptions    options,
        string                   accountId,
        CancellationToken        cancellationToken)
    {
        if(!_consentStore.HasConsented(accountId))
        {
            var consented = await _consentPrompt
                                  .RequestConsentAsync(ConsentMessage, cancellationToken)
                                  .ConfigureAwait(false);

            _consentStore.RecordConsent(accountId, consented);

            if(!consented)
            {
                throw new InvalidOperationException(
                    "The user declined consent for the insecure token cache fallback. " +
                    "Authentication cannot proceed without a secure token store.");
            }
        }

        var cachePath = Path.Combine(options.TokenCacheDirectory, options.TokenCacheFileName + ".insecure");

        app.UserTokenCache.SetBeforeAccess(args =>
        {
            if(File.Exists(cachePath))
            {
                args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(cachePath));
            }
        });

        app.UserTokenCache.SetAfterAccess(args =>
        {
            if(args.HasStateChanged)
            {
                Directory.CreateDirectory(options.TokenCacheDirectory);
                File.WriteAllBytes(cachePath, args.TokenCache.SerializeMsalV3());
            }
        });
    }

    private static async Task RegisterViaHelperAsync(
        StorageCreationProperties props,
        ITokenCache               tokenCache)
    {
        var helper = await MsalCacheHelper.CreateAsync(props).ConfigureAwait(false);
        helper.RegisterCache(tokenCache);
    }
}
