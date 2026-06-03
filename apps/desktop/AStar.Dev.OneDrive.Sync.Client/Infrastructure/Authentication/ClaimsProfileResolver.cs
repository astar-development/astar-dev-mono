using System.Security.Claims;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>Resolves display-name and email from an MSAL <see cref="ClaimsPrincipal"/>, falling back to a supplied value when the relevant claim is absent or empty.</summary>
internal static class ClaimsProfileResolver
{
    /// <summary>Returns the value of the <c>name</c> claim when present and non-empty; otherwise returns <paramref name="fallback"/>.</summary>
    internal static string ResolveDisplayName(ClaimsPrincipal? claims, string fallback) =>
        claims?.FindFirst("name")?.Value is { Length: > 0 } name ? name : fallback;

    /// <summary>Returns <c>preferred_username</c> if present and non-empty, then <c>email</c> if present and non-empty, otherwise <paramref name="fallback"/>.</summary>
    internal static string ResolveEmail(ClaimsPrincipal? claims, string fallback)
    {
        var candidate = claims?.FindFirst("preferred_username")?.Value
                        ?? claims?.FindFirst("email")?.Value;

        return string.IsNullOrEmpty(candidate) ? fallback : candidate;
    }
}
