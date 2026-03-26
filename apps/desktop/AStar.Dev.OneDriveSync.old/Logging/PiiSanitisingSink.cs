using System.Text.RegularExpressions;

namespace AStar.Dev.OneDriveSync.old.Logging;

/// <summary>
/// Static helpers that scrub PII (email addresses, OAuth tokens) from log text (LG-02).
/// Applied when converting log events to view models and when formatting file output.
/// </summary>
public static partial class PiiSanitiser
{
    public static string Sanitise(string text)
    {
        text = EmailRegex().Replace(text, "[email-redacted]");
        text = BearerTokenRegex().Replace(text, "Bearer [token-redacted]");
        text = OAuthTokenRegex().Replace(text, "$1\"[token-redacted]\"");
        return text;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"(""(?:access_token|refresh_token|id_token)""\s*:\s*)""[^""]+""", RegexOptions.Compiled)]
    private static partial Regex OAuthTokenRegex();
}
