using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenRootSettings
{
    [Fact]
    public void when_created_from_an_entity_then_every_root_value_is_copied()
    {
        var entity = CreateEntity(250f);

        var settings = RootSettings.From(entity);

        settings.Urls.ShouldBe(new WallhavenUrls(entity.BaseUrl, "https://wallhaven.cc/login", "top", "hot", "subscriptions"));
        (settings.ApiKey, settings.SearchString, settings.SearchStringPrefix, settings.SearchStringSuffix, settings.ImagePauseInSeconds).ShouldBe(("api-key", "cats", "prefix-", "-suffix", 5));
        settings.Pages.ShouldBe(new PageRanges(new PageRange(0, 20), new PageRange(0, 0), new PageRange(0, 0), new PageRange(7, 80)));
        settings.Browser.ShouldBe(new BrowserOptions(true, Option.Some(250f)));
    }

    [Fact]
    public void when_the_entity_has_no_slow_motion_delay_then_the_setting_is_none() =>
        RootSettings.From(CreateEntity(null)).Browser.SlowMotionDelay.ShouldBe(Option.None<float>());

    [Fact]
    public void when_the_entity_has_a_zero_slow_motion_delay_then_the_setting_is_some_zero() =>
        RootSettings.From(CreateEntity(0f)).Browser.SlowMotionDelay.ShouldBe(Option.Some(0f));

    [Fact]
    public void when_applied_to_an_entity_then_the_root_values_are_replaced_and_the_children_are_untouched()
    {
        var entity = CreateEntity(250f);
        var settings = new RootSettings(
            new WallhavenUrls(new Uri("https://other.example/api"), "https://other.example/login", "new-top", "new-hot", "new-subscriptions"),
            "new-key",
            "dogs",
            "new-prefix",
            "new-suffix",
            9,
            new PageRanges(new PageRange(4, 50), new PageRange(5, 60), new PageRange(6, 70), new PageRange(8, 90)),
            new BrowserOptions(false, Option.None<float>()));

        settings.ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.BaseUrl.ShouldBe(new Uri("https://other.example/api"));
        entity.LoginUrl.ShouldBe("https://other.example/login");
        entity.ApiKey.ShouldBe("new-key");
        entity.SearchString.ShouldBe("dogs");
        entity.TopWallpapers.ShouldBe("new-top");
        entity.SearchStringPrefix.ShouldBe("new-prefix");
        entity.SearchStringSuffix.ShouldBe("new-suffix");
        entity.Subscriptions.ShouldBe("new-subscriptions");
        entity.ImagePauseInSeconds.ShouldBe(9);
        entity.StartingPageNumber.ShouldBe(4);
        entity.TotalPages.ShouldBe(50);
        entity.SubscriptionsStartingPageNumber.ShouldBe(5);
        entity.SubscriptionsTotalPages.ShouldBe(60);
        entity.TopWallpapersStartingPageNumber.ShouldBe(6);
        entity.TopWallpapersTotalPages.ShouldBe(70);
        (entity.HotWallpapers, entity.HotWallpapersStartingPageNumber, entity.HotWallpapersTotalPages).ShouldBe(("new-hot", 8, 90));
        entity.UseHeadless.ShouldBeFalse();
        entity.SlowMotionDelay.ShouldBeNull();
        entity.SearchConfiguration.SearchTerm.ShouldBe("term");
        entity.UserConfiguration.Username.ShouldBe("user");
        entity.ScrapeDirectories.RootDirectory.ShouldBe("root");
    }

    private static ScrapeConfigurationEntity CreateEntity(float? slowMotionDelay)
    {
        var id = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(id)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), id, "user@example.test", "user", "user-key"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), id, "term", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), id, "root", "famous", "sub"),
            BaseUrl = new Uri("https://wallhaven.cc/api/v1"),
            LoginUrl = "https://wallhaven.cc/login",
            ApiKey = "api-key",
            SearchString = "cats",
            TopWallpapers = "top",
            HotWallpapers = "hot",
            HotWallpapersStartingPageNumber = 7,
            HotWallpapersTotalPages = 80,
            SearchStringPrefix = "prefix-",
            SearchStringSuffix = "-suffix",
            Subscriptions = "subscriptions",
            ImagePauseInSeconds = 5,
            TotalPages = 20,
            UseHeadless = true,
            SlowMotionDelay = slowMotionDelay
        };
    }
}
