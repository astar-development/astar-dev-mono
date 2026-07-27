using Blazored.LocalStorage;

namespace AStarDev.Web.Consent;

/// <summary>
/// Tracks whether the visitor has made a cookie-consent decision, mirroring the
/// previous Astro site's <c>CookieConsent.vue</c> behaviour. A <see langword="null"/>
/// <see cref="AnalyticsAccepted"/> means no decision has been recorded yet.
/// </summary>
public sealed class CookieConsentState(ILocalStorageService localStorage)
{
    private const string StorageKey = "cookie-consent-analytics";

    public bool? AnalyticsAccepted { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await localStorage.GetItemAsStringAsync(StorageKey);
        AnalyticsAccepted = stored switch
        {
            "true" => true,
            "false" => false,
            _ => null,
        };
    }

    public async Task SetPreferenceAsync(bool analyticsAccepted)
    {
        AnalyticsAccepted = analyticsAccepted;
        await localStorage.SetItemAsStringAsync(StorageKey, analyticsAccepted ? "true" : "false");
        OnChange?.Invoke();
    }

    public async Task ClearPreferenceAsync()
    {
        AnalyticsAccepted = null;
        await localStorage.RemoveItemAsync(StorageKey);
        OnChange?.Invoke();
    }
}
