// =============================================================================
// SPIKE 1 — Microsoft Graph delta query for incremental OneDrive sync
// =============================================================================
// ASSUMPTION: Microsoft Graph delta query is sufficient for incremental sync
//             (i.e. we can get only changed files since last sync, not the full tree)
//
// SETUP (one-time):
//   1. Go to https://aka.ms/AppRegistrations and register a new app
//   2. Platform: "Mobile and desktop applications"
//      Redirect URI: http://localhost
//   3. API permissions (delegated): Files.ReadWrite, User.Read
//   4. Copy the Application (client) ID and paste into ClientId below
//
// HOW TO RUN:
//   dotnet run
//   Authenticate in the browser when prompted.
//   Run again after making a change in OneDrive to see the delta in action.
//
// WHAT TO CHECK (tick each box):
//   [ ] First run: all items in the watched folder are returned
//   [ ] delta-token.txt is written after the first run
//   [ ] After making a change in OneDrive, re-running returns ONLY changed items
//   [ ] After no changes, re-running returns an empty change set
//   [ ] Delta token survives being serialised to a string and reloaded from disk
//   [ ] Paging works: if > 200 items exist, all pages are traversed
// =============================================================================

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Identity.Client;

// ── Configuration ─────────────────────────────────────────────────────────────
const string ClientId = "YOUR_APP_CLIENT_ID"; // <── paste your app registration ID here
const string Tenant   = "consumers";           // personal Microsoft accounts
const string DeltaTokenFile = "delta-token.txt";

var scopes = new[] { "Files.ReadWrite", "User.Read" };

// ── MSAL public client ─────────────────────────────────────────────────────────
var msalApp = PublicClientApplicationBuilder
    .Create(ClientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
    .WithDefaultRedirectUri()
    .Build();

async Task<string> GetAccessTokenAsync()
{
    var accounts = await msalApp.GetAccountsAsync();
    try
    {
        var silent = await msalApp.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
        return silent.AccessToken;
    }
    catch (MsalUiRequiredException)
    {
        var interactive = await msalApp.AcquireTokenInteractive(scopes).ExecuteAsync();
        return interactive.AccessToken;
    }
}

// ── HTTP helper ────────────────────────────────────────────────────────────────
// Using HttpClient directly avoids any uncertainty about Graph SDK v5 PageIterator
// type parameters — and demonstrates that the underlying REST API is straightforward.
using var http = new HttpClient();

async Task<JsonElement> GetJsonAsync(string url)
{
    var token = await GetAccessTokenAsync();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var json = await http.GetStringAsync(url);
    return JsonDocument.Parse(json).RootElement;
}

// ── Delta query with manual page iteration ────────────────────────────────────
async Task<(List<JsonElement> items, string? deltaLink)> FetchDeltaAsync(string startUrl)
{
    var items    = new List<JsonElement>();
    string? nextLink  = startUrl;
    string? deltaLink = null;

    while (nextLink is not null)
    {
        var page = await GetJsonAsync(nextLink);

        if (page.TryGetProperty("value", out var values))
            foreach (var item in values.EnumerateArray())
                items.Add(item.Clone());

        // Follow @odata.nextLink for paging
        nextLink  = page.TryGetProperty("@odata.nextLink",  out var nl) ? nl.GetString() : null;

        // @odata.deltaLink appears on the final page
        if (page.TryGetProperty("@odata.deltaLink", out var dl))
            deltaLink = dl.GetString();
    }

    return (items, deltaLink);
}

// ── Main logic ─────────────────────────────────────────────────────────────────
if (File.Exists(DeltaTokenFile))
{
    var storedToken = (await File.ReadAllTextAsync(DeltaTokenFile)).Trim();
    Console.WriteLine("Using stored delta token (incremental sync)...");
    Console.WriteLine($"Token (first 80 chars): {storedToken[..Math.Min(80, storedToken.Length)]}...");
    Console.WriteLine();

    var (changes, newDeltaLink) = await FetchDeltaAsync(storedToken);

    if (changes.Count == 0)
    {
        Console.WriteLine("✓ No changes since last sync — delta query correctly returns empty change set.");
    }
    else
    {
        Console.WriteLine($"✓ {changes.Count} change(s) detected since last sync:");
        foreach (var item in changes)
        {
            var name    = item.TryGetProperty("name",    out var n) ? n.GetString() : "(root)";
            var deleted = item.TryGetProperty("deleted", out _);
            var isFile  = item.TryGetProperty("file",    out _);
            var type    = deleted ? "DELETED" : isFile ? "FILE" : "FOLDER";
            Console.WriteLine($"  [{type}] {name}");
        }
    }

    if (newDeltaLink is not null)
    {
        await File.WriteAllTextAsync(DeltaTokenFile, newDeltaLink);
        Console.WriteLine($"\n✓ Delta token updated in {DeltaTokenFile}");
    }
    else
    {
        Console.WriteLine($"\n⚠ No new delta link in response — stored token unchanged.");
    }
}
else
{
    Console.WriteLine("First run — full sync to establish delta baseline...");
    Console.WriteLine();

    const string InitialUrl = "https://graph.microsoft.com/v1.0/me/drive/root/delta";
    var (items, deltaLink) = await FetchDeltaAsync(InitialUrl);

    Console.WriteLine($"✓ Initial sync: {items.Count} items in OneDrive root.");
    Console.WriteLine("  Sample items (up to 10):");
    foreach (var item in items.Take(10))
    {
        var name   = item.TryGetProperty("name",   out var n) ? n.GetString() : "(root)";
        var isFile = item.TryGetProperty("file",   out _);
        Console.WriteLine($"  [{(isFile ? "FILE  " : "FOLDER")}] {name}");
    }
    if (items.Count > 10)
        Console.WriteLine($"  ... and {items.Count - 10} more");

    if (deltaLink is not null)
    {
        await File.WriteAllTextAsync(DeltaTokenFile, deltaLink);
        Console.WriteLine($"\n✓ Delta token saved to {DeltaTokenFile}");
        Console.WriteLine("  Make a change in OneDrive, then run again to test incremental sync.");
    }
    else
    {
        Console.WriteLine("\n✗ No delta link returned — paging may be incomplete.");
    }
}
