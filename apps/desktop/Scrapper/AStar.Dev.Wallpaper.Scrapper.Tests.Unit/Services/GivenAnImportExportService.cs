using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Extensions.Time.Testing;
using Serilog;
using System.IO.Abstractions;
using System.Text.Json;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAnImportExportService
{
    private static readonly string scrapperDirectory = Path.GetDirectoryName(ApplicationMetadata.FileClassificationsExportFilePath)!;
    private static readonly string scrapeConfigScrapperDirectory = Path.GetDirectoryName(ApplicationMetadata.ScrapeConfigurationExportFilePath)!;
    private static readonly string scrapperTagsDirectory = Path.GetDirectoryName(ApplicationMetadata.ScrapedTagsExportFilePath)!;

    private const string CelebrityClassificationName = "Test Celebrity";
    private const string NormalClassificationName = "Test Normal";
    private const string ValidPassword = "super-secret-password";

    private const string ActionTagValue = "Action";
    private const string GenreCategory = "Genre";
    private const string ComedyTagValue = "Comedy";

    private const string ValidClassificationsJson = """
        [
          {
            "createdAt": "2026-06-20T10:11:12",
            "updatedAt": "2026-06-20T13:14:15",
            "id": 1,
            "name": "Test Celebrity",
            "celebrity": true,
            "includeInSearch": true,
            "fileNameParts": [
              {
                "createdAt": "0001-01-01T00:00:00",
                "updatedAt": "0001-01-01T00:00:00",
                "id": 1,
                "text": "Test Celebrity",
                "includeInSearch": true
              }
            ]
          },
          {
            "createdAt": "2026-06-20T10:11:12",
            "updatedAt": "2026-06-20T13:14:15",
            "id": 2,
            "name": "Test Normal",
            "celebrity": false,
            "includeInSearch": true,
            "fileNameParts": [
              {
                "createdAt": "0001-01-01T00:00:00",
                "updatedAt": "0001-01-01T00:00:00",
                "id": 2,
                "text": "Test Normal",
                "includeInSearch": true
              }
            ]
          }
        ]
        """;

    private const string ValidScrapeConfigJson = """
        {
          "connectionStrings": { "sqlite": "Data Source=test.db" },
          "userConfiguration": { "loginEmailAddress": "test@example.com", "username": "testuser", "password": "REDACTED", "sessionCookie": "REDACTED" },
          "searchConfiguration": {
            "baseUrl": "https://example.com",
            "apiKey": "REDACTED",
            "searchCategories": [{ "id": "cat1", "name": "Category 1", "lastKnownImageCount": 0, "lastPageVisited": 0, "totalPages": 10, "includeInSearch": true }],
            "searchString": "test",
            "topWallpapers": "",
            "searchStringPrefix": "",
            "searchStringSuffix": "",
            "subscriptions": "",
            "imagePauseInSeconds": 1,
            "startingPageNumber": 1,
            "totalPages": 10,
            "subscriptionsStartingPageNumber": 0,
            "subscriptionsTotalPages": 0,
            "topWallpapersTotalPages": 0,
            "topWallpapersStartingPageNumber": 0,
            "loginUrl": "",
            "useHeadless": true,
            "slowMotionDelay": null
          },
          "scrapeDirectories": { "rootDirectory": "/tmp/scrape", "baseSaveDirectory": "saves", "baseDirectory": "base", "baseDirectoryFamous": "famous", "subDirectoryName": "sub" }
        }
        """;

    private const string ValidTagsJson = """
        [
          {
            "value": "Action",
            "category": "Genre",
            "includeInSearch": true
          },
          {
            "value": "Comedy",
            "category": "Genre",
            "includeInSearch": false
          }
        ]
        """;

    private readonly MockFileSystem mockFileSystem;
    private readonly ILogger mockLogger;
    private readonly IImportExportService sut;
    private System.TimeProvider timeProvider;

    public GivenAnImportExportService()
    {
        timeProvider = new FakeTimeProvider();
        mockFileSystem = new MockFileSystem();
        mockLogger = Substitute.For<ILogger>();
        sut = new ImportExportService(mockFileSystem, timeProvider, mockLogger);
    }

    [Fact]
    public void when_importing_and_file_does_not_exist_then_failure_result_is_returned() =>
        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Fail<List<FileClassificationDomain>, string>>();

    [Fact]
    public void when_importing_and_file_does_not_exist_then_logger_receives_error_call()
    {
        sut.ImportFileClassificationsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_and_file_contains_null_json_then_failure_result_is_returned()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, "null");

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Fail<List<FileClassificationDomain>, string>>();
    }

    [Fact]
    public void when_importing_and_file_contains_null_json_then_logger_receives_error_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, "null");

        sut.ImportFileClassificationsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_valid_classifications_then_result_is_ok()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>();
    }

    [Fact]
    public void when_importing_valid_classifications_then_correct_count_is_returned()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>()
           .Value.Count.ShouldBe(2);
    }

    [Fact]
    public void when_importing_valid_classifications_then_celebrity_classification_name_is_mapped()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>()
           .Value[0].Name.ShouldBe(CelebrityClassificationName);
    }

    [Fact]
    public void when_importing_valid_classifications_then_normal_classification_name_is_mapped()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>()
           .Value[1].Name.ShouldBe(NormalClassificationName);
    }

    [Fact]
    public void when_exporting_classifications_then_file_is_written_to_expected_path()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);

        sut.ExportFileClassificationsToFile(CreateDomainClassifications());

        mockFileSystem.File.Exists(ApplicationMetadata.FileClassificationsExportFilePath).ShouldBeTrue();
    }

    [Fact]
    public void when_exporting_classifications_then_logger_receives_information_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);

        sut.ExportFileClassificationsToFile(CreateDomainClassifications());

        mockLogger.Received(1).Information(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_file_system_throws_during_export_then_exception_is_rethrown()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, timeProvider, mockLogger);

        var act = () => throwingSut.ExportFileClassificationsToFile([]);

        act.ShouldThrow<IOException>();
    }

    [Fact]
    public void when_file_system_throws_during_export_then_logger_receives_error_call()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, timeProvider, mockLogger);

        Should.Throw<IOException>(() => throwingSut.ExportFileClassificationsToFile([]));

        mockLogger.Received(1).Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_scrape_config_and_file_does_not_exist_then_failure_result_is_returned() =>
        sut.ImportScrapeConfigurationFromFile()
           .ShouldBeOfType<Fail<ScrapeConfigurationEntity, string>>();

    [Fact]
    public void when_importing_scrape_config_and_file_does_not_exist_then_logger_receives_error_call()
    {
        sut.ImportScrapeConfigurationFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_scrape_config_and_file_contains_null_json_then_failure_result_is_returned()
    {
        mockFileSystem.Directory.CreateDirectory(scrapeConfigScrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapeConfigurationExportFilePath, "null");

        sut.ImportScrapeConfigurationFromFile()
           .ShouldBeOfType<Fail<ScrapeConfigurationEntity, string>>();
    }

    [Fact]
    public void when_importing_scrape_config_and_file_contains_null_json_then_logger_receives_error_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapeConfigScrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapeConfigurationExportFilePath, "null");

        sut.ImportScrapeConfigurationFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_valid_scrape_config_then_result_is_ok()
    {
        SetupValidScrapeConfigImportFile();

        sut.ImportScrapeConfigurationFromFile()
           .ShouldBeOfType<Ok<ScrapeConfigurationEntity, string>>();
    }

    [Fact]
    public void when_importing_valid_scrape_config_then_correct_connection_string_is_mapped()
    {
        SetupValidScrapeConfigImportFile();

        sut.ImportScrapeConfigurationFromFile()
           .ShouldBeOfType<Ok<ScrapeConfigurationEntity, string>>()
           .Value.ConnectionStrings.Sqlite.ShouldBe("Data Source=test.db");
    }

    [Fact]
    public void when_importing_valid_scrape_config_then_password_field_is_preserved_from_db()
    {
        SetupValidScrapeConfigImportFile();

        sut.ImportScrapeConfigurationFromFile()
           .ShouldBeOfType<Ok<ScrapeConfigurationEntity, string>>()
           .Value.UserConfiguration.Password.ShouldBe(ApplicationMetadata.Redacted);
    }

    [Fact]
    public void when_exporting_scrape_config_then_file_is_written_to_expected_path()
    {
        mockFileSystem.Directory.CreateDirectory(scrapeConfigScrapperDirectory);

        sut.ExportScrapeConfigurationToFile(CreateScrapeConfigurationEntityWithSensitiveData());

        mockFileSystem.File.Exists(ApplicationMetadata.ScrapeConfigurationExportFilePath).ShouldBeTrue();
    }

    [Fact]
    public void when_exporting_scrape_config_then_logger_receives_information_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapeConfigScrapperDirectory);

        sut.ExportScrapeConfigurationToFile(CreateScrapeConfigurationEntityWithSensitiveData());

        mockLogger.Received(1).Information(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_exporting_scrape_config_then_password_is_redacted_in_exported_file()
    {
        mockFileSystem.Directory.CreateDirectory(scrapeConfigScrapperDirectory);

        sut.ExportScrapeConfigurationToFile(CreateScrapeConfigurationEntityWithSensitiveData());

        var json = mockFileSystem.File.ReadAllText(ApplicationMetadata.ScrapeConfigurationExportFilePath);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("userConfiguration").GetProperty("password").GetString()
           .ShouldBe(ApplicationMetadata.Redacted);
    }

    [Fact]
    public void when_file_system_throws_during_scrape_config_export_then_exception_is_rethrown()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, timeProvider, mockLogger);

        var act = () => throwingSut.ExportScrapeConfigurationToFile(CreateScrapeConfigurationEntityWithSensitiveData());

        act.ShouldThrow<IOException>();
    }

    [Fact]
    public void when_file_system_throws_during_scrape_config_export_then_logger_receives_error_call()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, timeProvider, mockLogger);

        Should.Throw<IOException>(() => throwingSut.ExportScrapeConfigurationToFile(CreateScrapeConfigurationEntityWithSensitiveData()));

        mockLogger.Received(1).Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_tags_and_file_does_not_exist_then_failure_result_is_returned() =>
        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Fail<List<ScrapedTagDomain>, string>>();

    [Fact]
    public void when_importing_tags_and_file_does_not_exist_then_logger_receives_error_call()
    {
        sut.ImportScrapedTagsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_tags_and_file_contains_null_json_then_failure_result_is_returned()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, "null");

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Fail<List<ScrapedTagDomain>, string>>();
    }

    [Fact]
    public void when_importing_tags_and_file_contains_null_json_then_logger_receives_error_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, "null");

        sut.ImportScrapedTagsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_valid_tags_then_result_is_ok()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>();
    }

    [Fact]
    public void when_importing_valid_tags_then_correct_count_is_returned()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value.Count.ShouldBe(2);
    }

    [Fact]
    public void when_importing_valid_tags_then_first_tag_value_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[0].Value.ShouldBe(ActionTagValue);
    }

    [Fact]
    public void when_importing_valid_tags_then_first_tag_category_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[0].Category.ShouldBe(GenreCategory);
    }

    [Fact]
    public void when_importing_valid_tags_then_first_tag_include_in_search_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[0].IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public void when_importing_valid_tags_then_second_tag_value_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[1].Value.ShouldBe(ComedyTagValue);
    }

    [Fact]
    public void when_importing_valid_tags_then_second_tag_category_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[1].Category.ShouldBe(GenreCategory);
    }

    [Fact]
    public void when_importing_valid_tags_then_second_tag_include_in_search_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[1].IncludeInSearch.ShouldBeFalse();
    }

    [Fact]
    public void when_exporting_tags_then_file_is_written_to_expected_path()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);

        sut.ExportScrapedTagsToFile(CreateDomainTags());

        mockFileSystem.File.Exists(ApplicationMetadata.ScrapedTagsExportFilePath).ShouldBeTrue();
    }

    [Fact]
    public void when_exporting_tags_then_logger_receives_information_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);

        sut.ExportScrapedTagsToFile(CreateDomainTags());

        mockLogger.Received(1).Information(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_file_system_throws_during_tag_export_then_exception_is_rethrown()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, timeProvider, mockLogger);

        var act = () => throwingSut.ExportScrapedTagsToFile([]);

        act.ShouldThrow<IOException>();
    }

    [Fact]
    public void when_file_system_throws_during_tag_export_then_logger_receives_error_call()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, timeProvider, mockLogger);

        Should.Throw<IOException>(() => throwingSut.ExportScrapedTagsToFile([]));

        mockLogger.Received(1).Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private void SetupValidImportFile()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, ValidClassificationsJson);
    }

    private void SetupValidScrapeConfigImportFile()
    {
        mockFileSystem.Directory.CreateDirectory(scrapeConfigScrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapeConfigurationExportFilePath, ValidScrapeConfigJson);
    }

    private void SetupValidTagsImportFile()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, ValidTagsJson);
    }

    private static List<FileClassificationDomain> CreateDomainClassifications() =>
    [
        new() { Id = 1, Name = CelebrityClassificationName, Celebrity = true,  IncludeInSearch = true },
        new() { Id = 2, Name = NormalClassificationName,    Celebrity = false, IncludeInSearch = true }
    ];

    private static ScrapeConfigurationEntity CreateScrapeConfigurationEntityWithSensitiveData() => new()
    {
        ConnectionStrings = new ConnectionStrings { Sqlite = "Data Source=production.db" },
        UserConfiguration = new UserConfiguration
        {
            LoginEmailAddress = "user@example.com",
            Username = "testuser",
            Password = ValidPassword,
            SessionCookie = "actual-session-cookie"
        },
        SearchConfiguration = new SearchConfiguration
        {
            BaseUrl = new Uri("https://example.com"),
            ApiKey = "actual-api-key"
        },
        ScrapeDirectories = new ScrapeDirectories { RootDirectory = "/tmp/scrape" }
    };

    private static List<ScrapedTagDomain> CreateDomainTags() =>
    [
        new() { Value = ActionTagValue, Category = GenreCategory, IncludeInSearch = true  },
        new() { Value = ComedyTagValue, Category = GenreCategory, IncludeInSearch = false }
    ];
}
