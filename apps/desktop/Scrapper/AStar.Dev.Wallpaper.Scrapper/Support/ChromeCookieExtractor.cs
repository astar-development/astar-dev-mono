using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class ChromeCookieExtractor
{
    private static readonly byte[] Iv = [.. Enumerable.Repeat((byte)0x20, 16)];

    // Chrome stores expiry as microseconds since 1601-01-01; Unix epoch is 1970-01-01
    private const long ChromeEpochOffsetSeconds = 11_644_473_600L;

    public static async Task<IReadOnlyList<Cookie>> ExtractAsync(string domain, Serilog.ILogger? logger = null)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "google-chrome", "Default", "Cookies");

        if(!File.Exists(dbPath)) return [];

        var tempPath = Path.Combine(Path.GetTempPath(), $"chrome_cookies_{Guid.NewGuid():N}.db");
        File.Copy(dbPath, tempPath, overwrite: true);

        try
        {
            var keystoreKey = await TryGetKeystoreKeyAsync();
            logger?.Debug("Keystore key {Status}", keystoreKey is null ? "NOT found — using peanuts fallback" : "found via GNOME keyring");
            var cookies     = new List<Cookie>();

            using var conn = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
            SELECT host_key, name, encrypted_value, path, expires_utc, is_secure, is_httponly
            FROM cookies
            WHERE host_key LIKE $pattern
            """;
            cmd.Parameters.AddWithValue("$pattern", $"%{domain.TrimStart('.')}%");

            using var reader = cmd.ExecuteReader();
            while(reader.Read())
            {
                var host      = reader.GetString(0);
                var name      = reader.GetString(1);
                var encrypted = reader.GetFieldValue<byte[]>(2);
                var path      = reader.GetString(3);
                var expires   = reader.GetInt64(4);
                var secure    = reader.GetBoolean(5);
                var httpOnly  = reader.GetBoolean(6);

                var value = Decrypt(encrypted, keystoreKey);
                if(string.IsNullOrEmpty(name)) continue;
                if(value is null)
                {
                    logger?.Debug("Cookie '{Name}' decrypt failed (null) — skipped", name);
                    continue;
                }
                logger?.Debug("Cookie '{Name}' value preview: [{Preview}]", name, value.Length > 20 ? value[..20] + "…" : value);

                var unixExpiry = expires > 0 ? expires / 1_000_000L - ChromeEpochOffsetSeconds : -1L;

                cookies.Add(new Cookie
                {
                    Name     = name,
                    Value    = value ?? string.Empty,
                    Domain   = host,
                    Path     = string.IsNullOrEmpty(path) ? "/" : path,
                    HttpOnly = httpOnly,
                    Expires  = unixExpiry > 0 ? (float)unixExpiry : -1,
                });
            }

            return cookies;
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static string? Decrypt(byte[] data, byte[]? keystoreKey)
    {
        if(data.Length == 0) return string.Empty;

        if(data.Length > 3)
        {
            var prefix = Encoding.ASCII.GetString(data, 0, 3);
            if(prefix == "v10") return AesCbcDecrypt(data[3..], DeriveKey("peanuts"));
            if(prefix == "v11") return AesCbcDecrypt(data[3..], keystoreKey ?? DeriveKey("peanuts"));
        }

        return Encoding.UTF8.GetString(data);
    }

    private static string? AesCbcDecrypt(byte[] ciphertext, byte[] key)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key     = key;
            aes.IV      = Iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var dec = aes.CreateDecryptor();
            var plain = dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
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

    private static async Task<byte[]?> TryGetKeystoreKeyAsync()
    {
        // When launched from IDE/terminal the D-Bus session socket is not inherited;
        // derive it from the UID so secret-tool can reach the GNOME keyring.
        var env = await BuildDbusEnvAsync();

        foreach(var lookupArgs in (string[])
        [
            "lookup xdg:schema chrome_libsecret_os_crypt_password_v2 application chrome",
            "lookup xdg:schema chrome_libsecret_os_crypt_password_v2 application chromium",
            "lookup application chrome",
        ])
        {
            var raw = await RunAsync("secret-tool", lookupArgs, env);
            if(raw is not null) return DeriveKey(raw.Trim());
        }

        return null;
    }

    private static async Task<Dictionary<string, string>> BuildDbusEnvAsync()
    {
        var env = new Dictionary<string, string>();

        // /proc/self/status gives us the real UID without P/Invoke
        try
        {
            var status  = await File.ReadAllTextAsync("/proc/self/status");
            var uidLine = status.Split('\n').FirstOrDefault(l => l.StartsWith("Uid:", StringComparison.Ordinal));
            if(uidLine is not null)
            {
                var uid      = uidLine.Split('\t', StringSplitOptions.RemoveEmptyEntries)[1];
                var sockPath = $"/run/user/{uid}/bus";
                if(File.Exists(sockPath))
                    env["DBUS_SESSION_BUS_ADDRESS"] = $"unix:path={sockPath}";
            }
        }
        catch { /* non-Linux or /proc unavailable */ }

        return env;
    }

    private static async Task<string?> RunAsync(string exe, string arguments, Dictionary<string, string>? extraEnv = null)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            if(extraEnv is not null)
                foreach(var (k, v) in extraEnv)
                    psi.Environment[k] = v;

            using var proc = Process.Start(psi);
            if(proc is null) return null;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch { return null; }
    }
}
