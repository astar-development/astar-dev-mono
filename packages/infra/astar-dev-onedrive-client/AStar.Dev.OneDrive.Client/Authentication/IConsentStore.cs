namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Persists per-account user-consent decisions for the insecure local
///     token-cache fallback (AU-03).
/// </summary>
/// <remarks>
///     A consent decision is stored against the account's unique identifier so
///     that multiple Microsoft accounts on the same machine each have an
///     independent opt-in record.
/// </remarks>
public interface IConsentStore
{
    /// <summary>
    ///     Returns <c>true</c> when the user associated with <paramref name="accountId"/>
    ///     has previously granted consent to use the insecure local cache fallback.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    bool HasConsented(string accountId);

    /// <summary>
    ///     Records the user's consent decision for <paramref name="accountId"/>.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <param name="consented"><c>true</c> if the user granted consent; <c>false</c> if denied.</param>
    void RecordConsent(string accountId, bool consented);
}
