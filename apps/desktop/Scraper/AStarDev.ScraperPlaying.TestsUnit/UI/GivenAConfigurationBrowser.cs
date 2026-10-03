using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAConfigurationBrowser
{
    private static readonly ScrapeConfigurationSummary Summary = new(new ScrapeConfigurationId(Guid.CreateVersion7()), "wallhaven.cc");
    private readonly FakeCatalogue catalogue = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly CapturingLogger<ConfigurationBrowser> logger = new();
    private readonly ConfigurationBrowser browser;

    public GivenAConfigurationBrowser() => browser = new(catalogue, new ConfigurationEditorWindowFactory(new ConfigurationEditSaver(new FakeUpdater()), new MockFileSystem()), status, logger);

    [Fact]
    public async Task when_the_configurations_are_listed_then_the_summaries_are_returned_without_any_status()
    {
        catalogue.Summaries = Exceptional.Success<IReadOnlyList<ScrapeConfigurationSummary>>([Summary]);

        var summaries = await browser.ListAsync();

        (summaries.Single(), status.Text).ShouldBe((Summary, string.Empty));
    }

    [Fact]
    public async Task when_listing_the_configurations_fails_then_none_are_returned_and_the_failure_is_reported()
    {
        catalogue.Summaries = new InvalidOperationException("list failed");

        var summaries = await browser.ListAsync();

        (summaries.Count, status.Text).ShouldBe((0, $"{TestStatusReporter.Timestamp} Unable to list scrape configurations. list failed"));
    }

    [Fact]
    public async Task when_a_configuration_is_found_to_edit_then_the_visit_is_logged_without_any_details()
    {
        catalogue.Found = Exceptional.Success((Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration());

        _ = await browser.FindAsync(Summary);

        logger.Entries.ShouldBe([(LogLevel.Information, "Page `Scrape configuration editor` viewed.")]);
    }

    [Fact]
    public async Task when_the_configuration_to_edit_is_not_found_then_no_visit_is_logged()
    {
        catalogue.Found = Exceptional.Success(Option.None<ScrapeConfigurationEntity>());

        _ = await browser.FindAsync(Summary);

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_configuration_is_found_then_it_is_returned_without_any_status()
    {
        var entity = new ScrapeConfigurationEntity(Summary.Id);
        catalogue.Found = Exceptional.Success(Option.Some(entity));

        var found = await browser.FindAsync(Summary);

        (found is Option<ScrapeConfigurationEntity>.Some some && ReferenceEquals(some.Value, entity), status.Text).ShouldBe((true, string.Empty));
    }

    [Fact]
    public async Task when_the_configuration_no_longer_exists_then_none_is_returned_and_the_user_is_told()
    {
        catalogue.Found = Exceptional.Success(Option.None<ScrapeConfigurationEntity>());

        var found = await browser.FindAsync(Summary);

        (found is Option<ScrapeConfigurationEntity>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} The selected scrape configuration could not be found."));
    }

    [Fact]
    public async Task when_loading_the_configuration_fails_then_none_is_returned_and_the_failure_is_reported()
    {
        catalogue.Found = new InvalidOperationException("load failed");

        var found = await browser.FindAsync(Summary);

        (found is Option<ScrapeConfigurationEntity>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} Unable to load scrape configuration. load failed"));
    }

    [Fact]
    public async Task when_a_configuration_is_loaded_for_editing_then_its_id_and_inputs_are_returned()
    {
        var entity = ScrapeConfiguration.GivenAConfigurationEditInputs.CreateEntity();
        catalogue.Found = Exceptional.Success(Option.Some(entity));

        var loaded = await browser.LoadAsync(Summary);

        var configuration = loaded.ShouldBeOfType<Option<LoadedConfiguration>.Some>().Value;
        configuration.Id.ShouldBe(entity.Id);
        ScrapeConfiguration.GivenAConfigurationEditInputs.ShouldMatchEntity(configuration.Inputs, entity);
    }

    [Fact]
    public async Task when_a_configuration_to_load_for_editing_cannot_be_found_then_none_is_returned_and_the_user_is_told()
    {
        catalogue.Found = Exceptional.Success(Option.None<ScrapeConfigurationEntity>());

        var loaded = await browser.LoadAsync(Summary);

        (loaded is Option<LoadedConfiguration>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} The selected scrape configuration could not be found."));
    }

    [Fact]
    public async Task when_there_are_no_configurations_to_edit_then_no_editor_is_created_and_the_user_is_told()
    {
        catalogue.Summaries = Exceptional.Success<IReadOnlyList<ScrapeConfigurationSummary>>([]);

        var editor = await browser.CreateEditorAsync();

        (editor is Option<ConfigurationEditorWindow>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} There are no scrape configurations to edit."));
    }

    [Fact]
    public async Task when_the_first_configuration_cannot_be_loaded_then_no_editor_is_created()
    {
        catalogue.Summaries = Exceptional.Success<IReadOnlyList<ScrapeConfigurationSummary>>([Summary]);
        catalogue.Found = Exceptional.Success(Option.None<ScrapeConfigurationEntity>());

        var editor = await browser.CreateEditorAsync();

        (editor is Option<ConfigurationEditorWindow>.None, status.Text).ShouldBe((true, $"{TestStatusReporter.Timestamp} The selected scrape configuration could not be found."));
    }

    private sealed class FakeUpdater : IScrapeConfigurationUpdater
    {
        public Task<Exceptional<Option<Unit>>> SaveAsync(ScrapeConfigurationId id, IReadOnlyList<IScrapeConfigurationSectionEdit> edits, CancellationToken cancellationToken)
            => Task.FromResult(Exceptional.Success(Option.Some(Unit.Instance)));
    }

    private sealed class FakeCatalogue : IScrapeConfigurationCatalogue
    {
        public Exceptional<IReadOnlyList<ScrapeConfigurationSummary>> Summaries { get; set; } = Exceptional.Success<IReadOnlyList<ScrapeConfigurationSummary>>([]);

        public Exceptional<Option<ScrapeConfigurationEntity>> Found { get; set; } = Exceptional.Success(Option.None<ScrapeConfigurationEntity>());

        public Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Summaries);

        public Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id, CancellationToken cancellationToken = default) => Task.FromResult(Found);
    }
}
