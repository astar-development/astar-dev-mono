namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>
/// Validates OData next-link URLs before following them, preventing open-redirect
/// attacks from malicious server responses.
/// </summary>
internal static class OdataNextLinkGuard
{
    private const string AllowedHost = "graph.microsoft.com";

    /// <summary>
    /// Returns <see langword="true"/> only when <paramref name="nextLink"/> is a valid
    /// absolute URI using HTTPS and pointing at <c>graph.microsoft.com</c>.
    /// </summary>
    /// <param name="nextLink">The OData next-link value from a Graph API response page.</param>
    internal static bool IsSafe(string? nextLink)
    {
        if (string.IsNullOrEmpty(nextLink))
            return false;

        if (!Uri.TryCreate(nextLink, UriKind.Absolute, out Uri? uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        return string.Equals(uri.Host, AllowedHost, StringComparison.OrdinalIgnoreCase);
    }
}
