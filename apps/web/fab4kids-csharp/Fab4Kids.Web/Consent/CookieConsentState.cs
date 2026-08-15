using AStar.Dev.FunctionalParadigm;
using Blazored.LocalStorage;

namespace Fab4Kids.Web.Consent;

/// <summary>
/// Tracks whether the visitor has made a cookie-consent decision, mirroring the
/// previous Astro site's <c>CookieBanner.tsx</c> behaviour. A <see cref="Option{T}.None"/>
/// <see cref="AnalyticsAccepted"/> means no decision has been recorded yet.
/// </summary>
public sealed class CookieConsentState(ILocalStorageService localStorage)
{
    private const string StorageKey = "fab4kids-cookie-consent";

    public Option<bool> AnalyticsAccepted { get; private set; } = Option.None<bool>();

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        string? stored = await localStorage.GetItemAsStringAsync(StorageKey);
        AnalyticsAccepted = stored switch
        {
            "accepted" => Option.Some(true),
            "declined" => Option.Some(false),
            _ => Option.None<bool>(),
        };
    }

    public async Task SetPreferenceAsync(bool analyticsAccepted)
    {
        AnalyticsAccepted = Option.Some(analyticsAccepted);
        await localStorage.SetItemAsStringAsync(StorageKey, analyticsAccepted ? "accepted" : "declined");
        OnChange?.Invoke();
    }

    public async Task ClearPreferenceAsync()
    {
        AnalyticsAccepted = Option.None<bool>();
        await localStorage.RemoveItemAsync(StorageKey);
        OnChange?.Invoke();
    }
}
