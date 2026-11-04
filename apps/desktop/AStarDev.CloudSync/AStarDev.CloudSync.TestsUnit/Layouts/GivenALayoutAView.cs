using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.Layouts;

public class GivenALayoutAView
{
    [AvaloniaFact]
    public async Task when_shown_at_the_default_window_size_then_the_account_cards_scroll()
    {
        var window = await CreateShownWindowAsync();

        var cards = window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards");

        cards.Extent.Height.ShouldBeGreaterThan(cards.Viewport.Height);
        cards.Viewport.Height.ShouldBeGreaterThan(0);
    }

    [AvaloniaFact]
    public async Task when_shown_then_a_card_is_rendered_for_every_visible_account()
    {
        var window = await CreateShownWindowAsync();

        window.GetVisualDescendants().OfType<Border>().Count(border => border.Classes.Contains("account-card")).ShouldBe(4);
    }

    [AvaloniaFact]
    public async Task when_there_are_more_accounts_than_fit_then_the_account_list_scrolls_in_a_bounded_viewport()
    {
        var source = Substitute.For<ISyncOverviewSource>();
        source.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(SyncOverviewFactory.Create([.. Enumerable.Range(0, 30).Select(_ => TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()))], [], [], [], []));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, TestData.CreateLocalization()) } };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var accountList = window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountList");
        accountList.Extent.Height.ShouldBeGreaterThan(accountList.Viewport.Height);
        accountList.Viewport.Height.ShouldBeGreaterThan(0);
        accountList.Viewport.Height.ShouldBeLessThan(window.Height);
    }

    [AvaloniaFact]
    public async Task when_shown_then_only_the_selected_account_row_is_highlighted()
    {
        var window = await CreateShownWindowAsync();

        var rows = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("account-row")).ToList();

        rows.Count.ShouldBe(4);
        rows.Where(row => row.Classes.Contains("selected")).ShouldHaveSingleItem().DataContext.ShouldBeOfType<LayoutAAccountCardViewModel>().Account.Name.ShouldBe("Work");
    }

    [AvaloniaFact]
    public async Task when_an_account_row_is_clicked_then_it_becomes_the_highlighted_row()
    {
        var window = await CreateShownWindowAsync();
        var photosRow = window.GetVisualDescendants().OfType<Button>().Single(button => button.Classes.Contains("account-row") && button.DataContext is LayoutAAccountCardViewModel { Account.Name: "Photos" });

        photosRow.Command.ShouldNotBeNull().Execute(photosRow.CommandParameter);
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("account-row") && button.Classes.Contains("selected")).ShouldHaveSingleItem().ShouldBeSameAs(photosRow);
    }

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
