namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Configuration for MSAL-based OneDrive authentication.
/// </summary>
public sealed record AuthenticationOptions
{
    /// <summary>
    ///     The Azure AD authority tenant for personal Microsoft accounts.
    /// </summary>
    /// <remarks>
    ///     Only the <c>consumers</c> tenant is supported (AU-01).
    ///     Work and school accounts are explicitly excluded.
    /// </remarks>
    public const string ConsumersTenant = "consumers";

    /// <summary>
    ///     Sentinel account identifier used during initial application configuration,
    ///     before any real Microsoft account has authenticated (AU-03).
    ///     Consent decisions recorded under this ID apply machine-wide and cover
    ///     the bootstrap window before the first interactive sign-in.
    /// </summary>
    public const string SystemAdminAccountId = "system-admin";

    /// <summary>
    ///     The MSAL application (client) ID from the Azure app registration.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    ///     Directory in which the token cache file is stored (AU-02).
    ///     Defaults to <c>%LOCALAPPDATA%/AStar.Dev.OneDriveSync</c>.
    /// </summary>
    public string TokenCacheDirectory { get; init; } = DefaultCacheDirectory();

    /// <summary>
    ///     Filename for the MSAL token cache (AU-02).
    /// </summary>
    public string TokenCacheFileName { get; init; } = "msal.cache";

    private static string DefaultCacheDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AStar.Dev.OneDriveSync");
}
