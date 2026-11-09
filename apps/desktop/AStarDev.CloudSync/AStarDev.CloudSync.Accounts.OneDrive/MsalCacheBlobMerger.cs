using System.Text.Json;
using System.Text.Json.Nodes;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Merges two serialised MSAL token caches (MSAL v3 JSON) so moving plaintext tokens into the keyring keeps the accounts already there.</summary>
internal static class MsalCacheBlobMerger
{
    /// <summary>Merges <paramref name="incoming"/> into <paramref name="existing"/>. For an entry in both, the incoming entry wins.</summary>
    /// <param name="existing">The cache already in the keyring; may be empty.</param>
    /// <param name="incoming">The cache from the plaintext file; may be empty.</param>
    /// <returns>The merged cache.</returns>
    public static byte[] Merge(byte[] existing, byte[] incoming)
    {
        var merged = ParseOrEmpty(existing);

        foreach (var section in ParseOrEmpty(incoming))
            merged[section.Key] = MergeSection(merged[section.Key], section.Value);

        return JsonSerializer.SerializeToUtf8Bytes(merged);
    }

    private static JsonNode? MergeSection(JsonNode? target, JsonNode? source)
    {
        if (target is not JsonObject targetEntries || source is not JsonObject sourceEntries)
            return source?.DeepClone();

        var merged = targetEntries.DeepClone().AsObject();

        foreach (var entry in sourceEntries)
            merged[entry.Key] = entry.Value?.DeepClone();

        return merged;
    }

    private static JsonObject ParseOrEmpty(byte[] blob) => blob.Length == 0 ? [] : JsonNode.Parse(blob)?.AsObject() ?? [];
}
