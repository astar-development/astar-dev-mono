using System.Security.Cryptography;
using System.Text;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Produces a stable fingerprint of a set of sync rules so a change to what is selected can be detected cheaply.</summary>
public static class RulesFingerprintCalculator
{
    /// <summary>Computes an order-insensitive, case-insensitive fingerprint over each rule's type and remote path. The remote item id is deliberately excluded because it is back-filled after the first enumeration.</summary>
    public static string Compute(IReadOnlyList<SyncRuleEntity> rules)
    {
        string canonical = string.Join('\n', rules.Select(rule => $"{rule.RuleType}|{rule.RemotePath.ToLowerInvariant()}").Order(StringComparer.Ordinal));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
