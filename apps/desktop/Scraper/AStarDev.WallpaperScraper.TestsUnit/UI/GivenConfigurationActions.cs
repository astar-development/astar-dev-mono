using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Operations;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.UI;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenConfigurationActions : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly FakeDialogHost dialogs = new();
    private readonly FakeImportService importService = new();
    private readonly FakeExportService exportService = new();
    private readonly FakeConfigurationCatalogue configurationCatalogue = new();
    private readonly ConfigurationActions actions;

    public GivenConfigurationActions()
        => actions = new(
            new UserOperationRunner(coordinator, status),
            status,
            new ConfigurationTransfer(importService, exportService),
            new ConfigurationBrowser(configurationCatalogue, new ConfigurationEditorWindowFactory(new ConfigurationEditSaver(new FakeUpdater()), new MockFileSystem()), status, NullLogger<ConfigurationBrowser>.Instance));

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_a_configuration_is_imported_then_the_user_is_told()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");

        await actions.ImportConfigurationAsync(dialogs);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Scrape configuration imported.");
        importService.ImportedPaths.ShouldBe(["path/to/import.json"]);
    }

    [Fact]
    public async Task when_no_file_is_picked_to_import_then_the_user_is_told_it_was_cancelled()
    {
        dialogs.PickedImportPath = Option.None<string>();

        await actions.ImportConfigurationAsync(dialogs);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Scrape configuration import cancelled.");
        importService.ImportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_import_fails_then_the_failure_is_reported_and_not_thrown()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");
        importService.Failure = new InvalidOperationException("import failed");

        await Should.NotThrowAsync(() => actions.ImportConfigurationAsync(dialogs));

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to import scrape configuration. import failed");
    }

    [Fact]
    public async Task when_the_import_is_cancelled_then_the_user_is_told()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");
        importService.Failure = new OperationCanceledException(new CancellationToken(true));
        dialogs.OnPick = () => coordinator.Cancel();

        await actions.ImportConfigurationAsync(dialogs);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Scrape configuration import cancelled.");
    }

    [Fact]
    public async Task when_another_operation_is_running_then_nothing_is_imported()
    {
        _ = coordinator.TryStart(out _);

        await actions.ImportConfigurationAsync(dialogs);

        (dialogs.PickCount, status.Text).ShouldBe((0, string.Empty));
    }

    [Theory]
    [InlineData(ApiKeyExport.Exclude, "Scrape configuration exported without its API keys.")]
    [InlineData(ApiKeyExport.Include, "Scrape configuration exported including its API keys.")]
    public async Task when_a_configuration_is_exported_then_the_user_is_told_whether_the_api_keys_were_included(ApiKeyExport apiKeys, string expected)
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.ConfigurationExists = true;

        await actions.ExportConfigurationAsync(dialogs, apiKeys);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} {expected}");
        exportService.ExportedPaths.ShouldBe(["path/to/export.json"]);
        exportService.ExportedModes.ShouldBe([apiKeys]);
    }

    [Fact]
    public async Task when_there_is_no_configuration_to_export_then_the_user_is_told()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.ConfigurationExists = false;

        await actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} No scrape configuration was found to export.");
    }

    [Fact]
    public async Task when_no_file_is_picked_to_export_then_the_user_is_told_it_was_cancelled()
    {
        dialogs.PickedExportPath = Option.None<string>();

        await actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Scrape configuration export cancelled.");
        exportService.ExportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_export_fails_then_the_failure_is_reported_and_not_thrown()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.Failure = new InvalidOperationException("export failed");

        await Should.NotThrowAsync(() => actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude));

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to export scrape configuration. export failed");
    }

    [Fact]
    public async Task when_the_export_is_cancelled_then_the_user_is_told()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.Failure = new OperationCanceledException(new CancellationToken(true));
        dialogs.OnPick = () => coordinator.Cancel();

        await actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Scrape configuration export cancelled.");
    }

    [Fact]
    public async Task when_there_are_no_configurations_to_edit_then_no_editor_is_shown_and_the_user_is_told()
    {
        await actions.EditConfigurationAsync(dialogs);

        (dialogs.ShownCount, status.Text).ShouldBe((0, $"{TestStatusReporter.Timestamp} There are no scrape configurations to edit."));
    }

    [Fact]
    public async Task when_the_configuration_editor_cannot_be_prepared_then_the_failure_is_reported_and_not_thrown()
    {
        configurationCatalogue.Failure = new InvalidOperationException("catalogue broke");

        await Should.NotThrowAsync(() => actions.EditConfigurationAsync(dialogs));

        (dialogs.ShownCount, status.Text).ShouldBe((0, $"{TestStatusReporter.Timestamp} Unable to edit scrape configuration. catalogue broke"));
    }

    private sealed class FakeImportService : IScrapeConfigurationImportService
    {
        public Exception? Failure { get; set; }

        public List<string> ImportedPaths { get; } = [];

        public Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException(Failure);

            ImportedPaths.Add(filePath);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeExportService : IScrapeConfigurationExportService
    {
        public Exception? Failure { get; set; }

        public bool ConfigurationExists { get; set; }

        public List<string> ExportedPaths { get; } = [];

        public List<ApiKeyExport> ExportedModes { get; } = [];

        public Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException<bool>(Failure);

            ExportedPaths.Add(filePath);
            ExportedModes.Add(apiKeys);

            return Task.FromResult(ConfigurationExists);
        }
    }

    private sealed class FakeUpdater : IScrapeConfigurationUpdater
    {
        public Task<Exceptional<Option<Unit>>> SaveAsync(ScrapeConfigurationId id, IReadOnlyList<IScrapeConfigurationSectionEdit> edits, CancellationToken cancellationToken)
            => Task.FromResult(Exceptional.Success(Option.Some(Unit.Instance)));
    }

    private sealed class FakeConfigurationCatalogue : IScrapeConfigurationCatalogue
    {
        public Exception? Failure { get; set; }

        public Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync(CancellationToken cancellationToken = default)
            => Failure is null
                ? Task.FromResult(Exceptional.Success<IReadOnlyList<ScrapeConfigurationSummary>>([]))
                : Task.FromException<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>>(Failure);

        public Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id, CancellationToken cancellationToken = default)
            => Task.FromResult(Exceptional.Success(Option.None<ScrapeConfigurationEntity>()));
    }
}
