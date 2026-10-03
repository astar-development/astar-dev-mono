using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using System.Text.Json;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationImport
{
    private readonly MockFileSystem fileSystem = new();

    [Fact]
    public async Task when_a_file_is_imported_then_the_repository_receives_the_complete_document()
    {
        var document = new ScrapeConfigurationImportDocument
        {
            ApiKey = "api-key",
            SearchConfiguration = new() { SearchCategories = [new() { Id = "general" }] }
        };
        var repository = new FakeImporter();
        var reader = new FakeReader { Document = document };
        var service = new ScrapeConfigurationImportService(repository, reader);

        await service.ImportAsync("configuration.json", TestContext.Current.CancellationToken);

        reader.ReadPaths.ShouldBe(["configuration.json"]);
        repository.Imported.ShouldBe([document]);
    }

    [Fact]
    public async Task when_the_repository_fails_to_import_then_the_failure_is_surfaced()
    {
        var failure = new InvalidOperationException("import rolled back");
        var service = new ScrapeConfigurationImportService(new FakeImporter { Failure = failure }, new FakeReader());

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => service.ImportAsync("configuration.json", TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_import_is_cancelled_then_file_processing_does_not_start()
    {
        var reader = new FakeReader();
        var service = new ScrapeConfigurationImportService(new FakeImporter(), reader);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ImportAsync("configuration.json", cancellationTokenSource.Token));

        reader.ReadPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_import_is_cancelled_while_reading_then_the_repository_is_not_updated()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new FakeImporter();
        var reader = new FakeReader { OnRead = () => cancellationTokenSource.Cancel() };
        var service = new ScrapeConfigurationImportService(repository, reader);

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ImportAsync("configuration.json", cancellationTokenSource.Token));

        repository.Imported.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_json_is_read_then_all_nested_configuration_values_are_preserved()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "userConfiguration": { "username": "user", "password": "secret" },
              "apiKey": "key", "baseUrl": "https://example.test", "loginUrl": "https://example.test/login",
              "searchConfiguration": {
                "searchCategories": [{ "id": "1", "name": "general", "isFamous": true }]
              },
              "scrapeDirectories": { "rootDirectory": "/tmp/scrapes" }
            }
            """, TestContext.Current.CancellationToken);

        var document = await new ScrapeConfigurationFileReader(fileSystem).ReadAsync(path, TestContext.Current.CancellationToken);

        document.UserConfiguration.Username.ShouldBe("user");
        document.ApiKey.ShouldBe("key");
        document.SearchConfiguration.SearchCategories.Single().IsFamous.ShouldBeTrue();
        document.ScrapeDirectories.RootDirectory.ShouldBe("/tmp/scrapes");
    }

    [Fact]
    public async Task when_application_settings_are_read_then_scrape_configuration_is_converted()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, """
                            {
                                "logging": { "logLevel": { "default": "Warning" } },
                                "scrapeConfiguration": {
                                    "userConfiguration": { "loginEmailAddress": "user@example.test", "username": "user", "password": "secret" },
                                    "searchConfiguration": {
                                        "baseUrl": "https://example.test", "loginUrl": "login",
                                        "searchCategories": [{ "id": "1", "name": "General", "lastPageVisited": 4, "totalPages": 8 }],
                                        "searchString": "/search", "imagePauseInSeconds": 10
                                    },
                                    "scrapeDirectories": {
                                        "baseSaveDirectory": "Pictures", "baseDirectory": "Pictures/Wallhaven",
                                        "baseDirectoryFamous": "Pictures/Famous", "subDirectoryName": "Wallhaven"
                                    }
                                }
                            }
                            """, TestContext.Current.CancellationToken);

        var document = await new ScrapeConfigurationFileReader(fileSystem).ReadAsync(path, TestContext.Current.CancellationToken);

        document.UserConfiguration.EmailAddress.ShouldBe("user@example.test");
        document.SearchConfiguration.SearchCategories.Single().LastPageVisited.ShouldBe(4);
        document.ScrapeDirectories.RootDirectory.ShouldBe("Pictures/Wallhaven");
        document.Id.ShouldNotBe(Guid.Empty);
        document.SearchConfiguration.Id.ShouldNotBe(Guid.Empty);
        document.BaseUrl.ShouldBe(new Uri("https://example.test"));
        document.SearchString.ShouldBe("/search");
        document.ImagePauseInSeconds.ShouldBe(10);
    }

    [Fact]
    public async Task when_json_is_malformed_then_reading_fails_with_a_json_exception()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, "not json", TestContext.Current.CancellationToken);

        await Should.ThrowAsync<JsonException>(() => new ScrapeConfigurationFileReader(fileSystem).ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void when_document_is_mapped_then_the_full_entity_graph_is_created()
    {
        var document = new ScrapeConfigurationImportDocument
        {
            Id = Guid.CreateVersion7(),
            UserConfiguration = new() { Username = "user" },
            SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [new() { Id = "1", Name = "General" }], PersonCategories = ["Drivers", "Models"] },
            ScrapeDirectories = new() { RootDirectory = "/tmp" }
        };

        var entity = document.ToEntity(DateTimeOffset.UnixEpoch);

        entity.UserConfiguration.Username.ShouldBe("user");
        entity.SearchConfiguration.SearchTerm.ShouldBe("cats");
        entity.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        entity.SearchConfiguration.PersonCategories.Select(category => category.Name).ShouldBe(["Drivers", "Models"]);
        entity.SearchConfiguration.PersonCategories.ShouldAllBe(category => category.SearchConfigurationId == entity.SearchConfiguration.Id);
        entity.ScrapeDirectories.RootDirectory.ShouldBe("/tmp");
    }

    [Fact]
    public async Task when_a_document_predates_person_categories_then_the_default_categories_are_used()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, """{ "UserConfiguration": {}, "SearchConfiguration": { "SearchTerm": "cats" }, "ScrapeDirectories": {}, "BaseUrl": "https://example.test", "LoginUrl": "login" }""", TestContext.Current.CancellationToken);

        var document = await new ScrapeConfigurationFileReader(fileSystem).ReadAsync(path, TestContext.Current.CancellationToken);

        document.SearchConfiguration.PersonCategories.ShouldBe(["Celebrities", "Models", "Pornstars", "Other Figures", "Actress"]);
    }

    [Fact]
    public async Task when_a_document_has_an_empty_person_category_list_then_it_stays_empty()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, """{ "UserConfiguration": {}, "SearchConfiguration": { "PersonCategories": [] }, "ScrapeDirectories": {}, "BaseUrl": "https://example.test", "LoginUrl": "login" }""", TestContext.Current.CancellationToken);

        var document = await new ScrapeConfigurationFileReader(fileSystem).ReadAsync(path, TestContext.Current.CancellationToken);

        document.SearchConfiguration.PersonCategories.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_older_file_still_contains_a_password_then_it_is_ignored_and_the_rest_is_imported()
    {
        var document = await ReadJsonAsync("""{ "userConfiguration": { "emailAddress": "user@example.test", "username": "user", "password": "old-secret" }, "searchConfiguration": {}, "scrapeDirectories": {}, "baseUrl": "https://example.test", "loginUrl": "login" }""");

        (document.UserConfiguration.EmailAddress, document.UserConfiguration.Username).ShouldBe(("user@example.test", "user"));
    }

    [Fact]
    public async Task when_a_document_is_written_then_the_file_contains_no_password()
    {
        const string path = "/configuration.json";
        await new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(new ScrapeConfigurationImportDocument { UserConfiguration = new UserConfigurationImportDocument { Username = "user" } }, path, TestContext.Current.CancellationToken);

        (await fileSystem.File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).Contains("password", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    [Theory]
    [InlineData("""{ "searchConfiguration": {}, "scrapeDirectories": {}, "baseUrl": "https://example.test", "loginUrl": "login" }""", "userConfiguration")]
    [InlineData("""{ "userConfiguration": {}, "scrapeDirectories": {}, "baseUrl": "https://example.test", "loginUrl": "login" }""", "searchConfiguration")]
    [InlineData("""{ "userConfiguration": {}, "searchConfiguration": {}, "baseUrl": "https://example.test", "loginUrl": "login" }""", "scrapeDirectories")]
    [InlineData("""{ "userConfiguration": {}, "searchConfiguration": {}, "scrapeDirectories": {}, "loginUrl": "login" }""", "baseUrl")]
    [InlineData("""{ "userConfiguration": {}, "searchConfiguration": {}, "scrapeDirectories": {}, "baseUrl": "https://example.test" }""", "loginUrl")]
    [InlineData("""{ "userConfiguration": null, "searchConfiguration": {}, "scrapeDirectories": {}, "baseUrl": "https://example.test", "loginUrl": "login" }""", "userConfiguration")]
    [InlineData("""{ "USERCONFIGURATION": {}, "SEARCHCONFIGURATION": {}, "scrapeDirectories": null, "BASEURL": "https://example.test", "LOGINURL": "login" }""", "scrapeDirectories")]
    public async Task when_a_required_value_is_missing_or_null_then_reading_fails_naming_it(string json, string missing)
    {
        var thrown = await Should.ThrowAsync<JsonException>(() => ReadJsonAsync(json));

        thrown.Message.ShouldContain(missing);
    }

    [Fact]
    public async Task when_several_required_values_are_missing_then_every_one_is_named()
    {
        var thrown = await Should.ThrowAsync<JsonException>(() => ReadJsonAsync("""{ "apiKey": "key" }"""));

        thrown.Message.ShouldBe("The configuration file is missing required values: userConfiguration, searchConfiguration, scrapeDirectories, baseUrl, loginUrl.");
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("null")]
    public async Task when_the_file_is_not_a_json_object_then_reading_fails_with_a_json_exception(string json)
        => _ = await Should.ThrowAsync<JsonException>(() => ReadJsonAsync(json));

    [Theory]
    [InlineData("""{ "scrapeConfiguration": { "searchConfiguration": { "baseUrl": "https://example.test", "loginUrl": "login" }, "scrapeDirectories": {} } }""", "scrapeConfiguration.userConfiguration")]
    [InlineData("""{ "scrapeConfiguration": { "userConfiguration": {}, "searchConfiguration": { "baseUrl": "https://example.test", "loginUrl": "login" } } }""", "scrapeConfiguration.scrapeDirectories")]
    [InlineData("""{ "scrapeConfiguration": { "userConfiguration": {}, "scrapeDirectories": {} } }""", "scrapeConfiguration.searchConfiguration")]
    [InlineData("""{ "scrapeConfiguration": { "userConfiguration": {}, "searchConfiguration": { "loginUrl": "login" }, "scrapeDirectories": {} } }""", "scrapeConfiguration.searchConfiguration.baseUrl")]
    [InlineData("""{ "scrapeConfiguration": { "userConfiguration": {}, "searchConfiguration": { "baseUrl": "https://example.test" }, "scrapeDirectories": {} } }""", "scrapeConfiguration.searchConfiguration.loginUrl")]
    public async Task when_a_required_application_settings_value_is_missing_then_reading_fails_naming_it(string json, string missing)
    {
        var thrown = await Should.ThrowAsync<JsonException>(() => ReadJsonAsync(json));

        thrown.Message.ShouldContain(missing);
    }

    private async Task<ScrapeConfigurationImportDocument> ReadJsonAsync(string json)
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);

        return await new ScrapeConfigurationFileReader(fileSystem).ReadAsync(path, TestContext.Current.CancellationToken);
    }

    private sealed class FakeImporter : IScrapeConfigurationImporter
    {
        public Exception? Failure { get; init; }

        public List<ScrapeConfigurationImportDocument> Imported { get; } = [];

        public Task<Exceptional<Unit>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document, CancellationToken cancellationToken = default)
        {
            Imported.Add(document);

            return Task.FromResult(Failure is null ? Exceptional.Success(Unit.Instance) : Exceptional.Failure<Unit>(Failure));
        }
    }

    private sealed class FakeReader : IScrapeConfigurationFileReader
    {
        public ScrapeConfigurationImportDocument Document { get; set; } = new();

        public Action OnRead { get; set; } = () => { };

        public List<string> ReadPaths { get; } = [];

        public Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ReadPaths.Add(filePath);
            OnRead();

            return Task.FromResult(Document);
        }
    }
}
