// =============================================================================
// SPIKE 3 — MSAL public client auth on Linux
// =============================================================================
// ASSUMPTION A: MSAL public client flow works on Linux without a local HTTP
//               redirect server (or that such a server is trivial to add)
// ASSUMPTION B: "Personal Microsoft account" = consumers tenant
//               (https://login.microsoftonline.com/consumers)
//
// SETUP (one-time):
//   1. Go to https://aka.ms/AppRegistrations and register a new app
//   2. Platform: "Mobile and desktop applications"
//      Redirect URI: http://localhost
//   3. API permissions (delegated): User.Read, Files.ReadWrite
//   4. Paste the Application (client) ID into ClientId below
//
// HOW TO RUN:
//   dotnet run
//   Authenticate in the browser when prompted.
//   Run again — token should be retrieved silently from cache.
//
// WHAT TO CHECK (tick each box):
//   [ ] Interactive auth opens system browser (not embedded WebView)
//   [ ] Auth completes successfully for a personal Microsoft account
//   [ ] The authority "consumers" correctly rejects work/school accounts
//   [ ] Token is cached: second run does NOT prompt for login
//   [ ] Token cache file is written to the expected path (see output)
//   [ ] Fallback to insecure cache works when keychain unavailable (see below)
//   [ ] User's display name and email are returned from /me
//   [ ] OneDrive drive ID is returned (confirms Files scope granted)
//
// TESTING THE INSECURE FALLBACK:
//   Set environment variable SPIKE_FORCE_INSECURE_CACHE=1 before running.
//   This simulates a keychain-unavailable scenario.
// =============================================================================

using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

const string ClientId = "YOUR_APP_CLIENT_ID"; // <── paste your app registration ID here
const string Tenant   = "consumers";           // personal Microsoft accounts only
var scopes = new[] { "User.Read", "Files.ReadWrite" };

// ── Token cache paths ──────────────────────────────────────────────────────────
var cacheDir  = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AStar.Dev.Spikes.MsalAuth");
const string CacheFileName = "msal.cache";
Directory.CreateDirectory(cacheDir);

Console.WriteLine($"Token cache directory: {cacheDir}");
Console.WriteLine($"Force insecure cache:  {Environment.GetEnvironmentVariable("SPIKE_FORCE_INSECURE_CACHE") == "1"}");
Console.WriteLine();

// ── MSAL public client ─────────────────────────────────────────────────────────
var app = PublicClientApplicationBuilder
    .Create(ClientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
    .WithDefaultRedirectUri()  // http://localhost — no embedded browser needed
    .Build();

// ── Token cache: try OS keychain, fall back to plaintext file ─────────────────
var forceInsecure = Environment.GetEnvironmentVariable("SPIKE_FORCE_INSECURE_CACHE") == "1";

var storageProperties = new StorageCreationPropertiesBuilder(CacheFileName, cacheDir)
    .WithLinuxKeyring(
        schemaName:        "com.astardev.spikes.msalauth",
        collection:        MsalCacheHelper.LinuxKeyRingDefaultCollection,
        secretLabel:       "MSAL token cache",
        attribute1:        new KeyValuePair<string, string>("Version", "1"),
        attribute2:        new KeyValuePair<string, string>("ProductGroup", "AStar.Dev"))
    .WithLinuxUnprotectedFile()   // fallback when keychain unavailable
    .Build();

try
{
    if (forceInsecure) throw new Exception("Forced insecure cache for spike testing.");

    var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
    cacheHelper.RegisterCache(app.UserTokenCache);
    Console.WriteLine("✓ Token cache registered with OS keychain (or keychain fallback).");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ Keychain unavailable ({ex.Message})");
    Console.WriteLine("  Falling back to insecure plaintext cache.");
    // In production, this requires explicit user opt-in (AU-03).
    // For the spike, we just proceed to validate it works at all.
    var insecurePath = Path.Combine(cacheDir, CacheFileName + ".insecure");
    app.UserTokenCache.SetBeforeAccess(args =>
    {
        if (File.Exists(insecurePath))
            args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(insecurePath));
    });
    app.UserTokenCache.SetAfterAccess(args =>
    {
        if (args.HasStateChanged)
            File.WriteAllBytes(insecurePath, args.TokenCache.SerializeMsalV3());
    });
    Console.WriteLine($"  Insecure cache path: {insecurePath}");
}

Console.WriteLine();

// ── Acquire token ──────────────────────────────────────────────────────────────
AuthenticationResult result;
var accounts = await app.GetAccountsAsync();

try
{
    result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
    Console.WriteLine("✓ Token acquired silently (cached).");
}
catch (MsalUiRequiredException)
{
    Console.WriteLine("Interactive login required — browser will open...");
    result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
    Console.WriteLine("✓ Token acquired interactively.");
}

// ── Validate token contents ────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"✓ Account:       {result.Account.Username}");
Console.WriteLine($"✓ Tenant ID:     {result.TenantId}");
Console.WriteLine($"✓ Token expires: {result.ExpiresOn:yyyy-MM-dd HH:mm:ss} UTC");

// Confirm it's a personal account (consumers tenant ID is well-known)
const string ConsumersTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
if (result.TenantId.Equals(ConsumersTenantId, StringComparison.OrdinalIgnoreCase))
    Console.WriteLine("✓ Confirmed: personal Microsoft account (consumers tenant).");
else
    Console.WriteLine($"⚠ Unexpected tenant ID — may be a work/school account: {result.TenantId}");

// ── Call Graph to confirm OneDrive access ──────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Calling Microsoft Graph /me and /me/drive to confirm token scope...");

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);

var meJson    = await http.GetStringAsync("https://graph.microsoft.com/v1.0/me");
var driveJson = await http.GetStringAsync("https://graph.microsoft.com/v1.0/me/drive");

var me    = JsonDocument.Parse(meJson).RootElement;
var drive = JsonDocument.Parse(driveJson).RootElement;

Console.WriteLine($"✓ Display name: {me.GetProperty("displayName").GetString()}");
Console.WriteLine($"✓ OneDrive ID:  {drive.GetProperty("id").GetString()}");
Console.WriteLine($"✓ Drive type:   {drive.GetProperty("driveType").GetString()}"); // expect "personal"

Console.WriteLine();
Console.WriteLine("All checks complete. Review the tick-list in the spike header.");
