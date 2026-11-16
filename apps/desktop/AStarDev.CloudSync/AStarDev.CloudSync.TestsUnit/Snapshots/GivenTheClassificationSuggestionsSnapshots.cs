using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheClassificationSuggestionsSnapshots
{
    private static readonly CategoryId Colours = ClassificationTestData.NewCategoryId();
    private static readonly CategoryId People = ClassificationTestData.NewCategoryId();

    private static ClassificationSuggestionsViewModel CreateList(FakeClassificationSuggestions service) =>
        new(service, new FakeCategoryStore(ClassificationTestData.Category(Colours, "Colour"), ClassificationTestData.Category(People, "Person"), ClassificationTestData.Category(ClassificationTestData.NewCategoryId(), "Place")), TestData.CreateLocalization());

    private static Window Host(ClassificationSuggestionsViewModel list) => new()
    {
        Content = new Border
        {
            Padding = new Avalonia.Thickness(16),
            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
            Child = new ClassificationSuggestionsContent { DataContext = list }
        }
    };

    private static FakeClassificationSuggestions SomeSuggestions() => new(SuggestionTestData.Plain("sunset", 1234), SuggestionTestData.FirstName("alice", 56), SuggestionTestData.Plain("harbour", 7));

    private static ThemeVariant Theme(string theme) => theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_suggestions_are_listed_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var list = CreateList(SomeSuggestions());
        await list.LoadCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-listed-{theme}", 900, 360);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_there_are_no_suggestions_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var list = CreateList(new FakeClassificationSuggestions());
        await list.LoadCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-empty-{theme}", 900, 240);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_scan_fails_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var service = SomeSuggestions();
        service.ScanOutcome = Result.Failure<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());
        var list = CreateList(service);
        await list.LoadCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-failed-{theme}", 900, 240);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_scan_is_cancelled_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var service = SomeSuggestions();
        service.ScanGate = new TaskCompletionSource();
        var list = CreateList(service);
        var running = list.LoadCommand.ExecuteAsync(null);
        list.CancelCommand.Execute(null);
        await running;

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-cancelled-{theme}", 900, 240);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_scan_is_running_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var service = SomeSuggestions();
        service.ScanGate = new TaskCompletionSource();
        var list = CreateList(service);
        var running = list.LoadCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-loading-{theme}", 900, 240);
        service.ScanGate.SetResult();
        await running;
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_picker_is_open_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var list = CreateList(SomeSuggestions());
        await list.LoadCommand.ExecuteAsync(null);
        await list.PromoteCommand.ExecuteAsync(list.Rows[1]);

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-picker-{theme}", 900, 480);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_a_promote_failed_then_the_row_shows_its_error_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var service = SomeSuggestions();
        service.FailPromote = true;
        var list = CreateList(service);
        await list.LoadCommand.ExecuteAsync(null);
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);
        await list.ChooseCategoryCommand.ExecuteAsync(list.Categories[0]);

        Snapshot.ShouldMatchBaseline(Host(list), Theme(theme), $"classification-suggestions-promote-failed-{theme}", 900, 360);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_tab_host_shows_the_suggestions_then_it_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var localization = TestData.CreateLocalization();
        var categories = new FakeCategoryStore(ClassificationTestData.Category(Colours, "Colour"));
        var terms = new ClassificationTermListViewModel(new FakeTermStore(ClassificationTestData.Term("teal", Colours)), categories, new FakeFileClassificationStore(), localization);
        var tabs = new ClassificationTabsViewModel(terms, new ClassificationSuggestionsViewModel(SomeSuggestions(), categories, localization), localization);
        await tabs.LoadCommand.ExecuteAsync(null);
        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
                Child = new ClassificationTabsContent { DataContext = tabs }
            }
        };

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"classification-tabs-suggestions-{theme}", 900, 360);
    }
}
