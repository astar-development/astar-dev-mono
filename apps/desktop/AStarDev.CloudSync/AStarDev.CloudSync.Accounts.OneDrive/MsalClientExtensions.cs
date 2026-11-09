using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive;

internal static class MsalClientExtensions
{
    internal static async Task<ProviderAccountKey> ToCachedKeyAsync(this IMsalClient msal, ProviderAccountKey key)
    {
        var cachedKeys = await msal.GetAccountKeysAsync().ConfigureAwait(false);

        return cachedKeys.FirstOrNone(cached => string.Equals(cached, key.Value, StringComparison.OrdinalIgnoreCase))
            .Match(cached => ProviderAccountKeyFactory.Create(cached).Match(cachedKey => cachedKey, _ => key), () => key);
    }
}
