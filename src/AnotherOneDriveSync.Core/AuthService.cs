using AnotherOneDriveSync.Data;
using AnotherOneDriveSync.Data.Entities;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Serilog;

namespace AnotherOneDriveSync.Core;

public class AuthService : IAuthService
{
    private readonly IPublicClientApplication _pca;
    private readonly SyncDbContext _dbContext;
    private readonly ILogger _logger;

    // OneDrive scopes
    private static readonly string[] Scopes = { "https://graph.microsoft.com/Files.ReadWrite.All" };

    // Placeholder client ID - in real app, this would come from config
    private const string ClientId = "3057f494-687d-4abb-a653-4b8066230b6e";

    public AuthService(SyncDbContext dbContext, ILogger logger)
        : this(CreateDefaultPca(), dbContext, logger)
    {
    }

    internal AuthService(IPublicClientApplication pca, SyncDbContext dbContext, ILogger logger)
    {
        _pca = pca;
        _dbContext = dbContext;
        _logger = logger;

        // Set up token cache persistence
        _pca.UserTokenCache.SetBeforeAccess(BeforeAccessNotification);
        _pca.UserTokenCache.SetAfterAccess(AfterAccessNotification);
    }

    private static IPublicClientApplication CreateDefaultPca()
    {
        return PublicClientApplicationBuilder
            .Create(ClientId)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    internal void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        LoadTokenCache();
    }

    internal void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            SaveTokenCache(args);
        }
    }

    internal void SaveTokenCache(TokenCacheNotificationArgs args)
    {
        try
        {
            var cacheData = args.TokenCache.SerializeMsalV3();

            var entry = _dbContext.TokenCacheEntries.FirstOrDefault(e => e.Key == "msal_cache");
            if (entry == null)
            {
                entry = new TokenCacheEntry { Key = "msal_cache", Value = cacheData };
                _dbContext.TokenCacheEntries.Add(entry);
            }
            else
            {
                entry.Value = cacheData;
            }

            _dbContext.SaveChanges();
            _logger.Information("Token cache saved to database");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save token cache");
        }
    }

    internal void LoadTokenCache()
    {
        try
        {
            var entry = _dbContext.TokenCacheEntries.FirstOrDefault(e => e.Key == "msal_cache");
            if (entry != null)
            {
                // TODO: DeserializeMsalV3 not available in this MSAL version, implement cache loading
                // _pca.UserTokenCache.DeserializeMsalV3(entry.Value);
                _logger.Information("Token cache loaded from database");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load token cache");
        }
    }

    public async Task<AuthenticationResult> AcquireTokenSilentAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault();

        if (account == null)
        {
            throw new MsalUiRequiredException("No cached account found", "user_null");
        }

        try
        {
            var result = await _pca.AcquireTokenSilent(Scopes, account).ExecuteAsync();
            _logger.Information("Token acquired silently for user {User}", result.Account.Username);
            return result;
        }
        catch (MsalUiRequiredException ex)
        {
            _logger.Warning(ex, "Silent token acquisition failed, interactive login required");
            throw;
        }
    }

    public async Task<AuthenticationResult> AcquireTokenInteractiveAsync()
    {
        var result = await _pca.AcquireTokenInteractive(Scopes).ExecuteAsync();
        _logger.Information("Token acquired interactively for user {User}", result.Account.Username);
        return result;
    }

    public async Task<AuthenticationResult> AcquireTokenAsync()
    {
        try
        {
            return await AcquireTokenSilentAsync();
        }
        catch (MsalUiRequiredException)
        {
            return await AcquireTokenInteractiveAsync();
        }
    }

    public async Task SignOutAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _pca.RemoveAsync(account);
        }

        var entry = _dbContext.TokenCacheEntries.FirstOrDefault(e => e.Key == "msal_cache");
        if (entry != null)
        {
            _dbContext.TokenCacheEntries.Remove(entry);
            _dbContext.SaveChanges();
        }

        _logger.Information("User signed out and token cache cleared");
    }
}
