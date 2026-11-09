namespace AStarDev.CloudSync.Sync.OneDrive;

internal static class ODataNextLinkGuard
{
    private const string GraphHost = "graph.microsoft.com";

    internal static bool IsAllowed(string? nextLink) =>
        Uri.TryCreate(nextLink, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, GraphHost, StringComparison.OrdinalIgnoreCase);
}
