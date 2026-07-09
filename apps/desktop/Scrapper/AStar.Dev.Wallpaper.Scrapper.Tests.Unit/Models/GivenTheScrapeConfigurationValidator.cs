using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Models;

public sealed class GivenTheScrapeConfigurationValidator
{
    [Fact]
    public void when_the_configuration_is_fully_populated_then_the_result_is_valid_and_carries_the_configuration()
    {
        var configuration = new ScrapeConfigurationBuilder().Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        actual.ShouldBeOfType<Valid<ScrapeConfiguration>>().Value.ShouldBeSameAs(configuration);
    }

    [Fact]
    public void when_the_search_configuration_is_missing_then_a_single_error_is_reported_for_it()
    {
        var configuration = new ScrapeConfigurationBuilder().Build() with { SearchConfiguration = null! };

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe(nameof(ScrapeConfiguration.SearchConfiguration));
    }

    [Fact]
    public void when_the_scrape_directories_are_missing_then_a_single_error_is_reported_for_them()
    {
        var configuration = new ScrapeConfigurationBuilder().Build() with { ScrapeDirectories = null! };

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe(nameof(ScrapeConfiguration.ScrapeDirectories));
    }

    [Fact]
    public void when_the_user_configuration_is_missing_then_a_single_error_is_reported_for_it()
    {
        var configuration = new ScrapeConfigurationBuilder().Build() with { UserConfiguration = null! };

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe(nameof(ScrapeConfiguration.UserConfiguration));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_base_url_is_missing_then_an_error_is_reported_for_it(string? baseUrl)
    {
        var configuration = new ScrapeConfigurationBuilder { SearchConfiguration = new SearchConfigurationBuilder { BaseUrl = baseUrl! }.Build() }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("SearchConfiguration.BaseUrl");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_login_url_is_missing_then_an_error_is_reported_for_it(string? loginUrl)
    {
        var configuration = new ScrapeConfigurationBuilder { SearchConfiguration = new SearchConfigurationBuilder { LoginUrl = loginUrl! }.Build() }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("SearchConfiguration.LoginUrl");
    }

    [Fact]
    public void when_the_image_pause_is_negative_then_an_error_is_reported_for_it()
    {
        var configuration = new ScrapeConfigurationBuilder { SearchConfiguration = new SearchConfigurationBuilder { ImagePauseInSeconds = -1 }.Build() }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("SearchConfiguration.ImagePauseInSeconds");
    }

    [Fact]
    public void when_the_starting_page_number_is_below_one_then_an_error_is_reported_for_it()
    {
        var configuration = new ScrapeConfigurationBuilder { SearchConfiguration = new SearchConfigurationBuilder { StartingPageNumber = 0 }.Build() }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("SearchConfiguration.StartingPageNumber");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_login_email_address_is_missing_then_an_error_is_reported_for_it(string? loginEmailAddress)
    {
        var configuration = new ScrapeConfigurationBuilder { UserConfiguration = new UserConfiguration(loginEmailAddress!, "username", "password", "session-cookie") }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("UserConfiguration.LoginEmailAddress");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_password_is_missing_then_an_error_is_reported_for_it(string? password)
    {
        var configuration = new ScrapeConfigurationBuilder { UserConfiguration = new UserConfiguration("user@example.test", "username", password!, "session-cookie") }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("UserConfiguration.Password");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_root_directory_is_missing_then_an_error_is_reported_for_it(string? rootDirectory)
    {
        var configuration = new ScrapeConfigurationBuilder { ScrapeDirectories = new ScrapeDirectories(rootDirectory!, "base-save-directory", "base-directory", "base-directory-famous", "sub-directory") }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("ScrapeDirectories.RootDirectory");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_base_save_directory_is_missing_then_an_error_is_reported_for_it(string? baseSaveDirectory)
    {
        var configuration = new ScrapeConfigurationBuilder { ScrapeDirectories = new ScrapeDirectories("root-directory", baseSaveDirectory!, "base-directory", "base-directory-famous", "sub-directory") }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.ShouldHaveSingleItem().Property.ShouldBe("ScrapeDirectories.BaseSaveDirectory");
    }

    [Fact]
    public void when_multiple_fields_are_invalid_then_all_errors_are_accumulated()
    {
        var configuration = new ScrapeConfigurationBuilder
        {
            SearchConfiguration = new SearchConfigurationBuilder { BaseUrl = "", LoginUrl = "", ImagePauseInSeconds = -1 }.Build(),
            UserConfiguration = new UserConfiguration("", "username", "", "session-cookie")
        }.Build();

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.Select(error => error.Property).ShouldBe(["SearchConfiguration.BaseUrl", "SearchConfiguration.LoginUrl", "SearchConfiguration.ImagePauseInSeconds", "UserConfiguration.LoginEmailAddress", "UserConfiguration.Password"]);
    }

    [Fact]
    public void when_all_sections_are_missing_then_one_error_is_reported_per_section()
    {
        var configuration = new ScrapeConfigurationBuilder().Build() with { SearchConfiguration = null!, UserConfiguration = null!, ScrapeDirectories = null! };

        var actual = ScrapeConfigurationValidator.Validate(configuration);

        var invalid = actual.ShouldBeOfType<Invalid<ScrapeConfiguration>>();
        invalid.Errors.Select(error => error.Property).ShouldBe([nameof(ScrapeConfiguration.SearchConfiguration), nameof(ScrapeConfiguration.UserConfiguration), nameof(ScrapeConfiguration.ScrapeDirectories)]);
    }
}
