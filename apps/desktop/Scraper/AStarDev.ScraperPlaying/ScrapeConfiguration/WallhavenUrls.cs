using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated Wallhaven addresses of a scrape configuration.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "The login page is free text and is not required to be a valid URL.")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "The login page is free text and is not required to be a valid URL.")]
public sealed record WallhavenUrls(Uri BaseUrl, string LoginUrl, string TopWallpapers, string HotWallpapers, string Subscriptions)
{
    /// <summary>Copies the addresses from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static WallhavenUrls From(ScrapeConfigurationEntity entity) => new(entity.BaseUrl, entity.LoginUrl, entity.TopWallpapers, entity.HotWallpapers, entity.Subscriptions);

    /// <summary>Replaces the addresses of the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to update.</param>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        entity.BaseUrl = BaseUrl;
        entity.LoginUrl = LoginUrl;
        entity.TopWallpapers = TopWallpapers;
        entity.HotWallpapers = HotWallpapers;
        entity.Subscriptions = Subscriptions;
    }

    /// <summary>Converts the addresses to their editable text form.</summary>
    public WallhavenUrlsInput ToInput() => new(BaseUrl.ToString(), LoginUrl, TopWallpapers, HotWallpapers, Subscriptions);
}

/// <summary>The unvalidated Wallhaven addresses as entered in the configuration editor.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "Holds raw text typed into the editor; parsed and validated by RootSettingsInput.Validate.")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Holds raw text typed into the editor; parsed and validated by RootSettingsInput.Validate.")]
public sealed record WallhavenUrlsInput(string BaseUrl, string LoginUrl, string TopWallpapers, string HotWallpapers, string Subscriptions);
