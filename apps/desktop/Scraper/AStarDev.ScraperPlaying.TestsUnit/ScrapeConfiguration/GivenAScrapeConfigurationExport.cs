using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using System.Runtime.Versioning;
using System.Text.Json;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationExport
{
    private readonly MockFileSystem fileSystem = new();

    [Fact]
    public async Task when_a_configuration_exists_then_it_is_written_to_the_destination_file()
    {
        var document = new ScrapeConfigurationImportDocument
        {
            ApiKey = "api-key",
            SearchConfiguration = new() { SearchCategories = [new() { Id = "general" }] }
        };
        var exporter = new FakeExporter { Result = (Option<ScrapeConfigurationImportDocument>)document };
        var writer = new FakeWriter();
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var exported = await service.ExportAsync("configuration.json", ApiKeyExport.Exclude, TestContext.Current.CancellationToken);

        exported.ShouldBeTrue();
        writer.Written.ShouldBe([(document, "configuration.json")]);
    }

    [Fact]
    public async Task when_no_configuration_exists_then_nothing_is_written()
    {
        var exporter = new FakeExporter { Result = Option<ScrapeConfigurationImportDocument>.None.Instance };
        var writer = new FakeWriter();
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var exported = await service.ExportAsync("configuration.json", ApiKeyExport.Exclude, TestContext.Current.CancellationToken);

        exported.ShouldBeFalse();
        writer.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_exception_propagates()
    {
        var exception = new InvalidOperationException("lookup failed");
        var exporter = new FakeExporter { Result = exception };
        var writer = new FakeWriter();
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => service.ExportAsync("configuration.json", ApiKeyExport.Exclude, TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(exception);
        writer.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_export_is_cancelled_then_the_export_does_not_start()
    {
        var exporter = new FakeExporter();
        var service = new ScrapeConfigurationExportService(exporter, new FakeWriter());
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ExportAsync("configuration.json", ApiKeyExport.Exclude, cancellationTokenSource.Token));

        exporter.LookupCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_document_is_written_then_the_json_file_contains_the_full_configuration()
    {
        const string path = "/configuration.json";
        var document = new ScrapeConfigurationImportDocument
        {
            UserConfiguration = new() { Username = "user" },
            SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [new() { Id = "1", Name = "General", IsFamous = true }] },
            ScrapeDirectories = new() { RootDirectory = "/tmp/scrapes" }
        };

        await new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(document, path, TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(await fileSystem.File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));

        json.RootElement.GetProperty("userConfiguration").GetProperty("username").GetString().ShouldBe("user");
        json.RootElement.GetProperty("searchConfiguration").GetProperty("searchTerm").GetString().ShouldBe("cats");
        json.RootElement.GetProperty("searchConfiguration").GetProperty("searchCategories")[0].GetProperty("isFamous").GetBoolean().ShouldBeTrue();
        json.RootElement.GetProperty("scrapeDirectories").GetProperty("rootDirectory").GetString().ShouldBe("/tmp/scrapes");
    }

    [Theory]
    [InlineData(ApiKeyExport.Exclude)]
    [InlineData(ApiKeyExport.Include)]
    public async Task when_exporting_then_the_api_key_mode_is_passed_to_the_exporter(ApiKeyExport apiKeys)
    {
        var exporter = new FakeExporter { Result = (Option<ScrapeConfigurationImportDocument>)new ScrapeConfigurationImportDocument() };

        _ = await new ScrapeConfigurationExportService(exporter, new FakeWriter()).ExportAsync("configuration.json", apiKeys, TestContext.Current.CancellationToken);

        exporter.Modes.ShouldBe([apiKeys]);
    }

    [Fact]
    public void when_api_keys_are_excluded_then_both_api_keys_in_the_document_are_empty_and_nothing_else_is_lost()
    {
        var document = EntityWithApiKeys().ToImportDocument(ApiKeyExport.Exclude);

        (document.ApiKey, document.UserConfiguration.ApiKey, document.UserConfiguration.Username, document.SearchString).ShouldBe((string.Empty, string.Empty, "user", "search"));
    }

    [Fact]
    public void when_api_keys_are_included_then_both_api_keys_are_in_the_document()
    {
        var document = EntityWithApiKeys().ToImportDocument(ApiKeyExport.Include);

        (document.ApiKey, document.UserConfiguration.ApiKey).ShouldBe(("scrape-key", "user-key"));
    }

    [Fact]
    public async Task when_api_keys_are_excluded_then_the_written_file_contains_neither_key()
    {
        const string path = "/configuration.json";
        await new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(EntityWithApiKeys().ToImportDocument(ApiKeyExport.Exclude), path, TestContext.Current.CancellationToken);
        var text = await fileSystem.File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        (text.Contains("scrape-key", StringComparison.Ordinal), text.Contains("user-key", StringComparison.Ordinal)).ShouldBe((false, false));
    }

    [Fact]
    public async Task when_a_document_is_written_then_no_partial_file_is_left_beside_it()
    {
        const string path = "/configuration.json";

        await new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(new ScrapeConfigurationImportDocument(), path, TestContext.Current.CancellationToken);

        fileSystem.Directory.GetFiles("/").ShouldBe([path]);
    }

    [Fact]
    public async Task when_writing_is_cancelled_then_an_existing_export_is_left_untouched_and_no_partial_file_remains()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, "previous export", TestContext.Current.CancellationToken);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(new ScrapeConfigurationImportDocument(), path, cancellationTokenSource.Token));

        (await fileSystem.File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldBe("previous export");
        fileSystem.Directory.GetFiles("/").ShouldBe([path]);
    }

    [Fact]
    public async Task when_writing_is_cancelled_then_no_file_is_created()
    {
        const string path = "/configuration.json";
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(new ScrapeConfigurationImportDocument(), path, cancellationTokenSource.Token));

        fileSystem.Directory.GetFiles("/").ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_document_is_written_over_an_existing_export_then_the_file_holds_the_new_content()
    {
        const string path = "/configuration.json";
        await fileSystem.File.WriteAllTextAsync(path, "previous export", TestContext.Current.CancellationToken);

        await new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(new ScrapeConfigurationImportDocument { ApiKey = "fresh" }, path, TestContext.Current.CancellationToken);

        (await fileSystem.File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldContain("fresh");
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public async Task when_a_document_is_written_on_unix_then_only_the_owner_can_read_or_write_the_file()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes do not apply on Windows.");
        const string path = "/configuration.json";

        await new ScrapeConfigurationFileWriter(fileSystem).WriteAsync(new ScrapeConfigurationImportDocument(), path, TestContext.Current.CancellationToken);

        fileSystem.File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static ScrapeConfigurationEntity EntityWithApiKeys()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            ApiKey = "scrape-key",
            SearchString = "search",
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "user-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "/tmp", "Pictures/Famous", "Wallhaven")
        };
    }

    [Fact]
    public void when_an_entity_is_mapped_then_the_full_document_graph_is_created()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var entity = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, [
                new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = "1", Name = "General" }
            ]),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "/tmp", "Pictures/Famous", "Wallhaven")
        };
        entity.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchConfigurationId, Name = "Drivers" });

        var document = entity.ToImportDocument(ApiKeyExport.Include);

        document.UserConfiguration.Username.ShouldBe("user");
        document.SearchConfiguration.SearchTerm.ShouldBe("cats");
        document.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        document.SearchConfiguration.PersonCategories.ShouldBe(["Drivers"]);
        document.ScrapeDirectories.RootDirectory.ShouldBe("/tmp");
    }

    private sealed class FakeExporter : IScrapeConfigurationExporter
    {
        public Exceptional<Option<ScrapeConfigurationImportDocument>> Result { get; set; } = Option<ScrapeConfigurationImportDocument>.None.Instance;

        public int LookupCount { get; private set; }

        public List<ApiKeyExport> Modes { get; } = [];

        public Task<Exceptional<Option<ScrapeConfigurationImportDocument>>> ExportScrapeConfigurationAsync(ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
        {
            LookupCount++;
            Modes.Add(apiKeys);

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeWriter : IScrapeConfigurationFileWriter
    {
        public List<(ScrapeConfigurationImportDocument Document, string FilePath)> Written { get; } = [];

        public Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default)
        {
            Written.Add((document, filePath));

            return Task.CompletedTask;
        }
    }
}
