using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.UI;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAConfigurationTransfer
{
    private readonly FakeDialogHost dialogs = new();
    private readonly FakeImporter importer = new();
    private readonly FakeExporter exporter = new();
    private readonly ConfigurationTransfer transfer;

    public GivenAConfigurationTransfer()
        => transfer = new(importer, exporter);

    [Fact]
    public async Task when_the_import_picker_is_cancelled_then_nothing_is_imported()
    {
        dialogs.PickedImportPath = Option.None<string>();

        var result = await transfer.ImportPickedFileAsync(dialogs, TestContext.Current.CancellationToken);

        (result is Option<Unit>.None).ShouldBeTrue();
        importer.ImportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_file_is_picked_to_import_then_it_is_imported()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");

        var result = await transfer.ImportPickedFileAsync(dialogs, TestContext.Current.CancellationToken);

        (result is Option<Unit>.Some).ShouldBeTrue();
        importer.ImportedPaths.ShouldBe(["path/to/import.json"]);
    }

    [Fact]
    public async Task when_the_import_throws_then_the_failure_reaches_the_caller()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");
        importer.Failure = new InvalidOperationException("import failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => transfer.ImportPickedFileAsync(dialogs, TestContext.Current.CancellationToken));

        thrown.Message.ShouldBe("import failed");
    }

    [Fact]
    public async Task when_the_export_picker_is_cancelled_then_nothing_is_exported()
    {
        dialogs.PickedExportPath = Option.None<string>();

        var result = await transfer.ExportPickedFileAsync(dialogs, ApiKeyExport.Include, TestContext.Current.CancellationToken);

        (result is Option<bool>.None).ShouldBeTrue();
        exporter.ExportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_export_throws_then_the_failure_reaches_the_caller()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exporter.Failure = new InvalidOperationException("export failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => transfer.ExportPickedFileAsync(dialogs, ApiKeyExport.Include, TestContext.Current.CancellationToken));

        thrown.Message.ShouldBe("export failed");
    }

    private sealed class FakeImporter : IScrapeConfigurationImportService
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

    private sealed class FakeExporter : IScrapeConfigurationExportService
    {
        public Exception? Failure { get; set; }

        public List<string> ExportedPaths { get; } = [];

        public Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException<bool>(Failure);

            ExportedPaths.Add(filePath);

            return Task.FromResult(true);
        }
    }
}
