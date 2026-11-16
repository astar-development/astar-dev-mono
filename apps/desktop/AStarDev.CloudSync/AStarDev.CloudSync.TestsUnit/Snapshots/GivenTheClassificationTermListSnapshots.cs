using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheClassificationTermListSnapshots
{
    private static readonly CategoryId Colours = ClassificationTestData.NewCategoryId();
    private static readonly CategoryId Cars = ClassificationTestData.NewCategoryId();

    private static async Task<Window> CreateWindowAsync(FakeTermStore terms, FakeFileClassificationStore files)
    {
        var list = new ClassificationTermListViewModel(terms, new FakeCategoryStore(ClassificationTestData.Category(Colours, "Colour"), ClassificationTestData.Category(Cars, "Car")), files, TestData.CreateLocalization());
        await list.LoadCommand.ExecuteAsync(null);

        return new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
                Child = new ClassificationTermListContent { DataContext = list }
            }
        };
    }

    private static FakeTermStore SomeTerms()
    {
        return new FakeTermStore(
            ClassificationTestData.Term("audi", Cars, TermSourceFactory.CreateUser(), TermStatusFactory.CreateIgnored()),
            ClassificationTestData.Term("bmw", Cars),
            ClassificationTestData.Term("crimson", Colours, TermSourceFactory.CreateAuto(), TermStatusFactory.CreateActive()),
            ClassificationTestData.Term("teal", Colours));
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_terms_are_listed_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var terms = SomeTerms();
        var files = new FakeFileClassificationStore();
        files.Counts[terms.Stored[0].Id] = 1234;
        files.Counts[terms.Stored[1].Id] = 56;
        var window = await CreateWindowAsync(terms, files);

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"classification-terms-listed-{theme}", 900, 360);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_there_are_no_terms_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var window = await CreateWindowAsync(new FakeTermStore(), new FakeFileClassificationStore());

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"classification-terms-empty-{theme}", 900, 240);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_terms_cannot_be_read_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var terms = new FakeTermStore { GetAllOutcome = Result.Failure<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()) };
        var window = await CreateWindowAsync(terms, new FakeFileClassificationStore());

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"classification-terms-failed-{theme}", 900, 240);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_a_toggle_failed_then_the_row_shows_its_error_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var terms = SomeTerms();
        terms.FailSetStatus = true;
        var window = await CreateWindowAsync(terms, new FakeFileClassificationStore());
        var list = (ClassificationTermListViewModel)((ClassificationTermListContent)((Border)window.Content!).Child!).DataContext!;

        await list.ToggleIgnoredCommand.ExecuteAsync(list.Rows[1]);

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"classification-terms-toggle-failed-{theme}", 900, 360);
    }

    private static ThemeVariant Theme(string theme) => theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;
}
