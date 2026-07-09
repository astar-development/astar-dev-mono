using System.Security.Cryptography;
using System.Text;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Support;

public static class ChromeCookieExtractor
{
    private static readonly byte[] Iv = [.. Enumerable.Repeat((byte)0x20, 16)];

    public static async Task<IReadOnlyList<Cookie>> ExtractAsync(string domain, string sessionId, TimeProvider clock)
    {
        return [(new Cookie
                {
                    Name = "wallhaven_session",
                    Value = sessionId,
                    Domain = domain,
                    Path = "/",
                    HttpOnly = true,
                    Expires = clock.GetUtcNow().AddDays(7).ToUnixTimeSeconds()
                })];
    }

    internal static IReadOnlyList<string> FindCookieDatabasePaths(string? homeDirectory = null)
    {
        string effectiveHomeDirectory = homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            string normalized = Path.GetFullPath(path);
            if (seen.Add(normalized))
            {
                candidates.Add(normalized);
            }
        }

        string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(configHome))
        {
            AddCandidate(Path.Combine(configHome, "google-chrome", "Default", "Cookies"));
            AddCandidate(Path.Combine(configHome, "google-chrome", "Profile 1", "Cookies"));
            AddCandidate(Path.Combine(configHome, "chromium", "Default", "Cookies"));
            AddCandidate(Path.Combine(configHome, "chromium", "Profile 1", "Cookies"));
        }

        string[] baseDirectories =
        [
            Path.Combine(effectiveHomeDirectory, ".config", "google-chrome"),
            Path.Combine(effectiveHomeDirectory, ".config", "chromium"),
            Path.Combine(effectiveHomeDirectory, ".var", "app", "com.google.Chrome", "config", "google-chrome"),
            Path.Combine(effectiveHomeDirectory, ".var", "app", "com.brave.Browser", "config", "brave"),
            Path.Combine(effectiveHomeDirectory, ".config", "microsoft-edge"),
        ];

        foreach (string baseDirectory in baseDirectories)
        {
            AddCandidate(Path.Combine(baseDirectory, "Default", "Cookies"));
            AddCandidate(Path.Combine(baseDirectory, "Profile 1", "Cookies"));
            AddCandidate(Path.Combine(baseDirectory, "Profile 2", "Cookies"));
            AddCandidate(Path.Combine(baseDirectory, "Profile 3", "Cookies"));
        }

        return candidates;
    }

    private static string? AesCbcDecrypt(byte[] ciphertext, byte[] key)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var dec = aes.CreateDecryptor();
            byte[] plain = dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            // Chrome 127+ prepends a 32-byte random nonce before the actual value
            return plain.Length > 32
                ? Encoding.UTF8.GetString(plain, 32, plain.Length - 32)
                : Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }

    private static byte[] DeriveKey(string password)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            Encoding.UTF8.GetBytes("saltysalt"),
            iterations: 1,
            hashAlgorithm: HashAlgorithmName.SHA1,
            outputLength: 16);
}
