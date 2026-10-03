using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAConfigurationEditSaver
{
    private readonly ScrapeConfigurationEntity configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2);
    private readonly FakeUpdater updater = new();
    private readonly ConfigurationEditSaver saver;

    public GivenAConfigurationEditSaver() => saver = new(updater);

    [Fact]
    public async Task when_every_section_is_valid_then_all_six_sections_are_saved_for_the_configuration_and_no_failure_is_returned()
    {
        var failure = await saver.SaveAsync(configuration.Id, ValidInputs(), CancellationToken.None);

        (failure, updater.SavedId, string.Join(",", updater.SavedEdits.Select(edit => edit.GetType().Name))).ShouldBe((Option.None<string>(), configuration.Id, "RootSettings,UserSettings,DirectorySettings,SearchSettings,SearchCategoriesSettings,PersonCategoriesSettings"));
    }

    [Fact]
    public async Task when_a_section_is_invalid_then_its_errors_are_returned_and_nothing_is_saved()
    {
        var inputs = ValidInputs() with { Root = ValidInputs().Root with { Urls = ValidInputs().Root.Urls with { BaseUrl = "not a url" } } };

        var failure = await saver.SaveAsync(configuration.Id, inputs, CancellationToken.None);

        (failure, updater.SaveCount).ShouldBe((Option.Some("BaseUrl: Must be an absolute http or https URL."), 0));
    }

    [Fact]
    public async Task when_several_sections_are_invalid_then_every_error_is_returned_one_per_line_in_section_order()
    {
        var valid = ValidInputs();
        var inputs = valid with { Root = valid.Root with { Urls = valid.Root.Urls with { BaseUrl = "not a url" } }, User = valid.User with { EmailAddress = "not an email" }, PersonCategories = new PersonCategoriesInput([new PersonCategoryInput(Option.None<Guid>(), " ")]) };

        var failure = await saver.SaveAsync(configuration.Id, inputs, CancellationToken.None);

        failure.ShouldBe(Option.Some($"BaseUrl: Must be an absolute http or https URL.{Environment.NewLine}EmailAddress: Must be a well-formed email address.{Environment.NewLine}PersonCategories[0].Name: Must not be blank."));
    }

    [Fact]
    public async Task when_the_configuration_no_longer_exists_then_that_is_returned()
    {
        updater.Result = Exceptional.Success(Option.None<Unit>());

        var failure = await saver.SaveAsync(configuration.Id, ValidInputs(), CancellationToken.None);

        failure.ShouldBe(Option.Some("The scrape configuration no longer exists."));
    }

    [Fact]
    public async Task when_saving_fails_then_the_failure_message_is_returned()
    {
        updater.Result = new InvalidOperationException("database is locked");

        var failure = await saver.SaveAsync(configuration.Id, ValidInputs(), CancellationToken.None);

        failure.ShouldBe(Option.Some("Unable to save the scrape configuration. database is locked"));
    }

    private ConfigurationEditInputs ValidInputs() => new(
        RootSettingsInput.From(configuration),
        UserSettingsInput.From(configuration),
        DirectorySettingsInput.From(configuration),
        SearchSettingsInput.From(configuration),
        SearchCategoriesInput.From(configuration),
        PersonCategoriesInput.From(configuration));

    private sealed class FakeUpdater : IScrapeConfigurationUpdater
    {
        public Exceptional<Option<Unit>> Result { get; set; } = Exceptional.Success(Option.Some(Unit.Instance));

        public ScrapeConfigurationId SavedId { get; private set; }

        public IReadOnlyList<IScrapeConfigurationSectionEdit> SavedEdits { get; private set; } = [];

        public int SaveCount { get; private set; }

        public Task<Exceptional<Option<Unit>>> SaveAsync(ScrapeConfigurationId id, IReadOnlyList<IScrapeConfigurationSectionEdit> edits, CancellationToken cancellationToken)
        {
            SaveCount++;
            SavedId = id;
            SavedEdits = edits;

            return Task.FromResult(Result);
        }
    }
}
