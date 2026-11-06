namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Merges two serialised MSAL token caches (MSAL v3 JSON) so moving plaintext tokens into the keyring keeps the accounts already there.</summary>
public static class MsalCacheBlobMerger
{
    /// <summary>Merges <paramref name="incoming"/> into <paramref name="existing"/>. For an entry in both, the incoming entry wins.</summary>
    /// <param name="existing">The cache already in the keyring; may be empty.</param>
    /// <param name="incoming">The cache from the plaintext file; may be empty.</param>
    /// <returns>The merged cache.</returns>
    public static byte[] Merge(byte[] existing, byte[] incoming) => throw new NotImplementedException();
}
