using System.Security.Cryptography;
using System.Text;

namespace AStarDev.LoggingExtensions;

/// <summary>Creates short, stable hashes of personal values (paths, account ids) so log records can be correlated without exposing the value (LG-02).</summary>
public static class LogHash
{
    /// <summary>Returns the first 16 lower-case hex characters of the SHA-256 of <paramref name="value" />. The hash is case-sensitive and unsalted, so it pseudonymises but does not anonymise.</summary>
    /// <param name="value">The personal value to hash.</param>
    /// <returns>The 16-character hash.</returns>
    public static string Of(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))[..16];
}
