using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Models;

public sealed class GivenTheScrapeErrorFactory
{
    [Fact]
    public void when_creating_a_page_load_failed_error_then_the_url_is_preserved() =>
        ScrapeErrorFactory.CreatePageLoadFailed("https://example.test/page", "could not load").Url.ShouldBe("https://example.test/page");

    [Fact]
    public void when_creating_a_page_load_failed_error_then_the_message_is_preserved() =>
        ScrapeErrorFactory.CreatePageLoadFailed("https://example.test/page", "could not load").Message.ShouldBe("could not load");

    [Fact]
    public void when_creating_a_page_parse_failed_error_then_the_header_text_is_preserved() =>
        ScrapeErrorFactory.CreatePageParseFailed("some header", "could not parse").HeaderText.ShouldBe("some header");

    [Fact]
    public void when_creating_a_page_parse_failed_error_with_a_null_header_text_then_the_header_text_is_null() =>
        ScrapeErrorFactory.CreatePageParseFailed(null, "could not parse").HeaderText.ShouldBeNull();

    [Fact]
    public void when_creating_an_image_download_failed_error_then_the_image_url_is_preserved() =>
        ScrapeErrorFactory.CreateImageDownloadFailed("https://example.test/image.jpg", "download failed").ImageUrl.ShouldBe("https://example.test/image.jpg");

    [Fact]
    public void when_creating_an_image_save_failed_error_then_the_path_is_preserved() =>
        ScrapeErrorFactory.CreateImageSaveFailed("/some/path/image.jpg", "save failed").Path.ShouldBe("/some/path/image.jpg");

    [Fact]
    public void when_creating_a_configuration_save_failed_error_then_the_message_is_preserved() =>
        ScrapeErrorFactory.CreateConfigurationSaveFailed("configuration save failed").Message.ShouldBe("configuration save failed");

    [Fact]
    public void when_creating_a_classification_failed_error_then_the_file_name_is_preserved() =>
        ScrapeErrorFactory.CreateClassificationFailed("image.jpg", "classification failed").FileName.ShouldBe("image.jpg");

    [Fact]
    public void when_creating_an_unexpected_error_then_the_exception_is_preserved()
    {
        var exception = new InvalidOperationException("boom");

        ScrapeErrorFactory.CreateUnexpectedError(exception).Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void when_creating_an_unexpected_error_then_the_message_is_the_exception_message() =>
        ScrapeErrorFactory.CreateUnexpectedError(new InvalidOperationException("boom")).Message.ShouldBe("boom");

    [Fact]
    public void when_creating_an_image_dimension_read_failed_error_then_the_path_is_preserved() =>
        ScrapeErrorFactory.CreateImageDimensionReadFailed("/some/path/image.jpg", "could not read dimensions").Path.ShouldBe("/some/path/image.jpg");

    [Fact]
    public void when_creating_an_image_dimension_read_failed_error_then_the_message_is_preserved() =>
        ScrapeErrorFactory.CreateImageDimensionReadFailed("/some/path/image.jpg", "could not read dimensions").Message.ShouldBe("could not read dimensions");

    [Fact]
    public void when_creating_an_import_failed_error_then_the_file_path_is_preserved() =>
        ScrapeErrorFactory.CreateImportFailed("/some/path/export.json", "file not found").FilePath.ShouldBe("/some/path/export.json");

    [Fact]
    public void when_creating_an_import_failed_error_then_the_message_is_preserved() =>
        ScrapeErrorFactory.CreateImportFailed("/some/path/export.json", "file not found").Message.ShouldBe("file not found");

    [Fact]
    public void when_creating_a_validation_failed_error_then_the_errors_are_preserved()
    {
        ValidationError[] errors = [new("Name", "Name is required"), new("Level", "Level must be between 1 and 3"),];

        ScrapeErrorFactory.CreateValidationFailed(errors, "validation failed").Errors.ShouldBe(errors);
    }

    [Fact]
    public void when_creating_a_validation_failed_error_then_the_message_is_preserved()
    {
        ValidationError[] errors = [new("Name", "Name is required"),];

        ScrapeErrorFactory.CreateValidationFailed(errors, "validation failed").Message.ShouldBe("validation failed");
    }
}
