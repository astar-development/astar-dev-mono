using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;
using AStarDev.WallpaperScraper.UI;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenAConfigurationSwitch
{
    private static readonly ScrapeConfigurationSummary Requested = new(new ScrapeConfigurationId(Guid.CreateVersion7()), "other.example");

    private readonly LoadedConfiguration current = LoadedConfiguration.From(GivenAConfigurationEditInputs.CreateEntity());

    [Fact]
    public async Task when_the_edits_can_be_discarded_and_the_load_succeeds_then_the_requested_configuration_is_loaded()
    {
        var requested = new LoadedConfiguration(Requested.Id, current.Inputs);

        var result = await ConfigurationSwitch.ResolveAsync(Requested, () => Task.FromResult(true), Session(_ => Task.FromResult(Option.Some(requested))));

        (result.Loaded is Option<LoadedConfiguration>.Some some && ReferenceEquals(some.Value, requested), result.Error is Option<string>.None).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_the_edits_are_not_discarded_then_nothing_is_loaded_and_no_error_is_shown()
    {
        var loadCalled = false;

        var result = await ConfigurationSwitch.ResolveAsync(Requested, () => Task.FromResult(false), Session(_ =>
        {
            loadCalled = true;

            return Task.FromResult(Option.None<LoadedConfiguration>());
        }));

        (result.Loaded is Option<LoadedConfiguration>.None, result.Error is Option<string>.None, loadCalled).ShouldBe((true, true, false));
    }

    [Fact]
    public async Task when_the_configuration_could_not_be_loaded_then_nothing_is_loaded_so_the_picker_can_revert()
    {
        var result = await ConfigurationSwitch.ResolveAsync(Requested, () => Task.FromResult(true), Session(_ => Task.FromResult(Option.None<LoadedConfiguration>())));

        (result.Loaded is Option<LoadedConfiguration>.None, result.Error is Option<string>.None).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_the_load_throws_then_nothing_is_loaded_and_the_failure_is_shown()
    {
        var result = await ConfigurationSwitch.ResolveAsync(Requested, () => Task.FromResult(true), Session(_ => throw new InvalidOperationException("load failed")));

        (result.Loaded is Option<LoadedConfiguration>.None, result.Error).ShouldBe((true, Option.Some("Unable to load the scrape configuration. load failed")));
    }

    [Fact]
    public async Task when_the_confirmation_throws_then_nothing_is_loaded_and_the_dialog_failure_is_shown_without_calling_it_a_load_failure()
    {
        var result = await ConfigurationSwitch.ResolveAsync(Requested, () => throw new InvalidOperationException("dialog failed"), Session(_ => Task.FromResult(Option.None<LoadedConfiguration>())));

        (result.Loaded is Option<LoadedConfiguration>.None, result.Error).ShouldBe((true, Option.Some("Unable to confirm discarding the unsaved edits. dialog failed")));
    }

    private ConfigurationEditorSession Session(Func<ScrapeConfigurationSummary, Task<Option<LoadedConfiguration>>> load) => new(current, [Requested], load);
}
