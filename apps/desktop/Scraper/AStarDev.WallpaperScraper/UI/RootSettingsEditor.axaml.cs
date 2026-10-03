using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Edits the root-level settings of a scrape configuration.</summary>
public sealed partial class RootSettingsEditor : UserControl
{
    public RootSettingsEditor() => InitializeComponent();

    /// <summary>Fills the controls from the specified settings.</summary>
    /// <param name="input">The settings to display.</param>
    public void Load(RootSettingsInput input)
    {
        LoadUrls(input.Urls);
        LoadPages(input.Pages);
        ApiKeyBox.Text = input.ApiKey;
        SearchStringBox.Text = input.SearchString;
        SearchStringPrefixBox.Text = input.SearchStringPrefix;
        SearchStringSuffixBox.Text = input.SearchStringSuffix;
        ImagePauseInSecondsBox.Value = input.ImagePauseInSeconds;
        UseHeadlessCheckBox.IsChecked = input.Browser.UseHeadless;
        SlowMotionDelayBox.Value = input.Browser.SlowMotionDelay.Match(delay => (decimal?)delay, () => null);
    }

    /// <summary>Reads the settings currently entered in the controls.</summary>
    public RootSettingsInput ReadInput() => new(
        ReadUrls(),
        ApiKeyBox.Text ?? string.Empty,
        SearchStringBox.Text ?? string.Empty,
        SearchStringPrefixBox.Text ?? string.Empty,
        SearchStringSuffixBox.Text ?? string.Empty,
        ToInt(ImagePauseInSecondsBox),
        ReadPages(),
        new BrowserOptions(UseHeadlessCheckBox.IsChecked == true, SlowMotionDelayBox.Value is { } delay ? Option.Some((float)delay) : Option.None<float>()));

    private void LoadUrls(WallhavenUrlsInput urls)
    {
        BaseUrlBox.Text = urls.BaseUrl;
        LoginUrlBox.Text = urls.LoginUrl;
        TopWallpapersBox.Text = urls.TopWallpapers;
        HotWallpapersBox.Text = urls.HotWallpapers;
        SubscriptionsBox.Text = urls.Subscriptions;
    }

    private void LoadPages(PageRanges pages)
    {
        (StartingPageNumberBox.Value, TotalPagesBox.Value) = (pages.Search.Start, pages.Search.Total);
        (SubscriptionsStartingPageNumberBox.Value, SubscriptionsTotalPagesBox.Value) = (pages.Subscriptions.Start, pages.Subscriptions.Total);
        (TopWallpapersStartingPageNumberBox.Value, TopWallpapersTotalPagesBox.Value) = (pages.TopWallpapers.Start, pages.TopWallpapers.Total);
        (HotWallpapersStartingPageNumberBox.Value, HotWallpapersTotalPagesBox.Value) = (pages.HotWallpapers.Start, pages.HotWallpapers.Total);
    }

    private WallhavenUrlsInput ReadUrls() => new(
        BaseUrlBox.Text ?? string.Empty,
        LoginUrlBox.Text ?? string.Empty,
        TopWallpapersBox.Text ?? string.Empty,
        HotWallpapersBox.Text ?? string.Empty,
        SubscriptionsBox.Text ?? string.Empty);

    private PageRanges ReadPages() => new(
        new PageRange(ToInt(StartingPageNumberBox), ToInt(TotalPagesBox)),
        new PageRange(ToInt(SubscriptionsStartingPageNumberBox), ToInt(SubscriptionsTotalPagesBox)),
        new PageRange(ToInt(TopWallpapersStartingPageNumberBox), ToInt(TopWallpapersTotalPagesBox)),
        new PageRange(ToInt(HotWallpapersStartingPageNumberBox), ToInt(HotWallpapersTotalPagesBox)));

    private static int ToInt(NumericUpDown control) => (int)(control.Value ?? 0);
}
